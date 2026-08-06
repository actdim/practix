# Decisions (ADR — append-only)

_One dated entry per architectural decision. Never edit past entries; mark a replaced one "Superseded by #N"._

<!-- Template:
## #001 — <title>
- Date: YYYY-MM-DD
- Status: accepted            (or: superseded by #NNN)
- Context: <why this came up>
- Decision: <what was decided>
- Consequences: <trade-offs / follow-ups>
-->

## #001 — A registry record without content is a transient state, not a valid blob
- Date: 2026-08-05
- Status: accepted
- Context: The registry (`blob_records`) and the data store (files) are separate layers, so a record can exist while its content does not — either because `TryGetOrSetAsync` only registers the record and the caller never writes, or because the file was removed out of band. `VerifyExistsAsync` treated that state as `KeyNotFound` and deleted the record, which silently destroyed metadata and made the whole `TryGetOrSetAsync` → write flow fail on any test that skipped the write step.
- Decision: Every entry point now runs through `ReconcileContentAsync(result, allowNew, timeout, ct)` — named for what it does, since it mutates both the result and the registry rather than merely checking:
  - `TryGetOrSetAsync` (all four overloads) passes `allowNew: true`. A missing blob is reported by setting `BlobResult.IsNew = true` on the result whose lock we already hold — the caller learns the content has to be produced, and no record is lost.
  - `TryGetForReadingAsync` / `TryGetForWritingAsync` pass `allowNew: false`. They were asked for existing content, so the orphaned record is deleted and `KeyNotFound` returned. An `IsNew` result reaching them is an impossible state and throws `InvalidOperationException`.
  - The orphan deletion honours the caller's timeout via the new `IBlobRegistry.DeleteAsync(key, timeout, ct)` overload; a `TimeoutException` from it is converted to `BlobErrorCode.Timeout`, never swallowed into `KeyNotFound` — a lock held by someone else means we could not establish whether the record is orphaned at all, and the caller deserves a reason to retry.
- Consequences:
  - `BlobResult.IsNew` now means "there is no content yet", covering both a fresh record and one that outlived its blob. Its constructor became `internal` and the setter `internal` so only the library can produce results.
  - Callers must write content before relying on `TryGetFor*`; tests seed via `TestEnvironment.SeedAsync`.
  - Ported from `CanarySystems.FileStorage` but reimplemented: the original released the lock and re-acquired it (a race window), set `IsNew` on results that could be `Timeout`/`KeyNotFound`, and threaded a `timeout` parameter that every call site passed as `null`.
  - `IBlobManager.DeleteAsync` gained a `timeout` overload as well, so every public entry point that locks a specific key now exposes one. `DeleteExpiredAsync` / `DeleteOlderThanAsync` / `CleanupAsync` deliberately do not — they work through SQL conditions without per-key locks.

## #002 — `BlobRecord.Size` is owned by the library and read from the data store, not the registry
- Date: 2026-08-05
- Status: accepted
- Context: `Size` was mapped to and from the `size` column but never computed anywhere, so it stayed `NULL` forever unless a caller assigned the field by hand. That is not merely cosmetic: `AppendAsync(offset)` requires `offset == length` to append, and the length has no other source, so an unpopulated `Size` makes appending impossible. A stale value is worse still — after the record outlives its content, the size of the vanished blob would be reported as current.
- Decision:
  - `IBlobDataStore.ExistsAsync` is replaced by `Task<long?> GetSizeAsync(record, ct)`, where `null` means "no content". Existence and size come from a single round trip, which also suits object stores where one `HEAD` answers both. **Amended later the same day**: `ExistsAsync` came back as a *default interface implementation* returning `GetSizeAsync(...).HasValue`, for callers that only want the fact. `GetSizeAsync` stays the single primitive a store implements, so the two cannot disagree and no backend is tempted to spend a second round trip on existence.
  - `null` versus `0` is load-bearing: a size of `0` is an existing zero-byte blob, which a caller can legitimately produce by opening `WriteAsync` and writing nothing. Conflating the two would make `TryGetFor*` prune the record of an empty blob.
  - `ReconcileContentAsync` assigns `record.Size` from the data store on every hand-out. Because the record is handed out under a lock held until dispose, that value is authoritative for the lifetime of the handle, not a best-effort snapshot.
  - `TrackSizeOnDispose` chains `record.OnDisposeAsync` for write-locked records to re-read the size before the registry persists the record, so the `size` column matches reality for readers that never take a handle (`QueryAsync` and future reporting). Read locks are skipped — content cannot change under one.
  - `BlobRecord.Size` setter became `internal`: the library observes the size, callers do not declare it. `Hash` stays caller-declared via `BlobStoreOptions` because computing it means reading the whole blob, which must not happen implicitly on every write.
- Consequences:
  - The dispose-time refresh depends on the caller disposing the write stream before the record. That is the documented usage pattern but cannot be enforced through the current `Stream`-returning API.
  - `Hash` is still never computed from content; `FileSystemBlobDataStore.ComputeXxHash3Async` remains commented out.
  - Discovered while doing this: `DeleteAsync` removes the registry row and the locks but nothing ever deletes the stored bytes — `IBlobDataStore` has no delete operation at all. Recreating the same key then reports `IsNew` while a stale file sits on disk. Needs a separate task.

## #003 — `BlobRecord` stays decoupled from the streams handed out for it
- Date: 2026-08-05
- Status: accepted
- Context: Both the write lock and `Size` (#002) are only correct if the caller disposes the write stream before the record. That could be enforced by letting the record own the streams it was used to open and closing them in `BlobRecord.DisposeAsync`, which would make lock and size correct by construction.
- Decision: Do not do that. `BlobRecord` remains a lightweight near-POCO; stream lifetime is a separate concern owned by the caller. The ordering stays a documented convention.
- Consequences:
  - Both idiomatic forms already produce the right order — a nested `await using` block, and `using` declarations in one scope (disposed in reverse order of declaration). Breaking it requires deliberately disposing the record while a stream is open.
  - The failure mode is not primarily a wrong `Size`: disposing the record releases the distributed lock, so writing after that point is a correctness bug regardless. `Size` merely turned a silent race into a visible wrong number.
  - Do not "fix" this later by giving `BlobRecord` a collection of open streams — that was considered and rejected here.

## #004 — Deletion is orchestrated by `BlobManager`, content before metadata
- Date: 2026-08-05
- Status: accepted
- Context: `IBlobDataStore` had no delete operation, so every deletion path removed metadata and left the bytes on disk forever — unbounded growth, worst under TTL/sliding-expiration cache usage where records expire constantly. Recreating a deleted key then reported `IsNew` while stale content sat on disk. See task `delete-blob-content`.
- Decision:
  - `IBlobDataStore.DeleteAsync(record, ct)` requires a write lock, like every other mutating operation, and returns whether anything was removed. `FileSystemBlobDataStore` also prunes the shard directories it empties.
  - `BlobManager` owns all four deletion paths, being the only layer that sees both the registry and the data store. The registry lost its key-based `DeleteAsync` / `DeleteExpiredAsync` / `DeleteOlderThanAsync` and gained `DeleteLockedAsync(record)` (deletes rows while the caller holds the write lock, attested by `record.LockType`), `ForceUnlockAsync(key)`, and two candidate-selection queries.
  - Order is content first, then metadata: a leftover registry row is recoverable — #001 reports it as `IsNew` and `TryGetFor*` prunes it — whereas a leftover file is invisible to the library.
  - Bulk deletion selects candidates with the registry's existing lock-excluding SQL, then runs each key through the same single-key routine with a zero lock timeout, so a key locked since selection is skipped rather than waited on. `forceDeleteLocked` breaks existing locks first.
  - This required fixing what a zero timeout means. The acquisition loop treated `timeout <= TimeSpan.Zero` as "unspecified" and substituted the default, so "do not wait" was originally expressed as 1 ms — which also only worked probabilistically, since an attempt finishing in under a millisecond sent the loop through another 100 ms delay. Now only a **negative** value means unspecified, and `TimeSpan.Zero` attempts once and gives up by construction: the deadline is already reached when the first attempt returns. The lock's own TTL stays a separate concern with its 1 s floor.
- Consequences:
  - Bulk deletion is N lock acquisitions instead of one SQL statement. Accepted: the alternative deletes content out from under a live reader.
  - `DeleteAsync` still signals through exceptions (`KeyNotFoundException`, `TimeoutException`) rather than `BlobErrorCode`, because it returns `Task`. Consistent within itself, unlike the `TryGet*` family.
  - `ReconcileContentAsync` reuses `DeleteCoreAsync` for orphan pruning, so that path now cleans up content as well.

## #005 — The data store exposes two write operations, neither taking a position
- Date: 2026-08-05
- Status: accepted
- Context: `CreateAsync` (`FileMode.Create`) and `WriteAsync` (`FileMode.Truncate`) produced identical results and differed only in whether the file had to pre-exist, which forced the caller to know whether the key was new — the original trap that started this work. `AppendAsync(record, offset)` additionally required the caller to know the current size, and permitted `offset` past the end, which silently zero-fills a hole.
- Decision:
  - `CreateAsync` is removed. `WriteAsync` becomes `FileMode.Create` — create-or-truncate — so it is correct for a new and an existing key alike, and the caller never consults `IsNew` to pick a write method.
  - `AppendAsync` loses its `offset` parameter and uses `FileMode.Append`, which creates the file when absent and positions at the end. The store knows the size; the caller does not have to.
  - Rejected: merging both into `WriteAsync(record, offset)` with "discard whatever follows what I wrote". One rule, but it needs a wrapper stream calling `SetLength` on dispose, and it hides the capability difference behind a parameter value — an object-store backend can implement full replace (`PutObject`) while refusing append, which is legible in two methods and not in one.
  - Rejected: `WriteAtAsync` as a name for positioned writing — one letter apart from `WriteAsync` for very different semantics.
- Consequences:
  - Writing at an arbitrary position with the tail preserved is no longer possible. Resumable upload — the use case that motivated `offset` — still works: `record.Size` (#002) tells the caller how many bytes are already stored, and the rest is a plain append. Patching the middle of a blob would have to come back as its own explicit operation.
  - `FileMode.Truncate`'s implicit "must exist" assertion is gone. It was redundant on the sanctioned path anyway, since `ReconcileContentAsync` already establishes existence under the lock.
  - The write surface is now `WriteAsync` / `AppendAsync` / `ReadAsync` / `DeleteAsync` / `GetSizeAsync` / `ResolveLocationAsync`, with no mode flags or enums — the operation is the method. Their direction was inverted afterwards by #006.

## #007 — Options are instructions, the record is state; `Apply` moves to `BlobRecord`
- Date: 2026-08-06
- Status: accepted
- Context: `BlobRecord` had public setters for everything, which looked like a hole next to `Size` being `internal` (#002). It is not: a write lock is exclusive, so mutating a record you hold and having it persisted on dispose is precisely what the lock is for. What was actually wrong was an asymmetry — `BlobStoreOptions` could be applied through `TryGetOrSetAsync` but not through `TryGetForWritingAsync`, even though the lock is the same, so changing an existing blob's content type meant a needless get-or-set.
- Decision:
  - **Setter visibility follows what the value is, not whether mutation is safe.** Facts the library observes are `internal`: `Size`, `Hash` (once computed — see `content-hash`), `CreatedAt`/`UpdatedAt`/`AccessedAt`, and `Key`. Intent the caller declares stays public: `ContentType`, `Metadata`, `SlidingExpiration`, `ExpiresAt`. A caller can only lie about the first group; the library cannot derive the second.
  - `ApplyOptions` moves out of `SQLiteBlobRegistry` and becomes `BlobRecord.Apply(BlobStoreOptions)`, public and requiring a write lock. The registry keeps an `internal Apply(options, now)` overload — it applies options while a record is still being set up and `LockType` is not decided yet, and it passes its own `now` so every timestamp derived in one operation agrees.
  - `BlobStoreOptions` is therefore **not** a creation-time convenience: it is the instruction type. `Ttl` is relative and has no representation on the record; "apply only what was set" and the AbsoluteExpiration > Ttl > SlidingExpiration priority are rules, not assignments. Dropping options in favour of plain setters would push the instruction-to-state translation onto the caller.
- Consequences:
  - Metadata on an existing blob is now updated under the write lock it was handed out with. `TryGetOrSetAsync`'s options parameter is, in effect, sugar for get-or-create plus `Apply`.
  - Applying options is no longer the registry's business, which is right — it never was persistence logic, only computation over a record plus the current time.
  - Pre-existing hole left untouched: `UpdateOnReadDisposeAsync` also persists the record, so mutations made under a **read** lock reach storage even though other readers may hold the same key. The public `Apply` guards against it; the plain setters do not. Worth closing separately.

## #006 — Writes consume a stream; reads hand one out
- Date: 2026-08-06
- Status: accepted
- Context: `WriteAsync` / `AppendAsync` returned a writable stream for the caller to push into. That made the content non-existent until the caller disposed that stream, which is what forced the disposal-order convention (#003) and the dispose-time size refresh (#002). On an object store it is worse than a wrong `Size`: a multipart upload does not exist until `CompleteMultipartUpload`, so a mis-ordered dispose means a **missing object with the lock already released**, plus incomplete parts billing until aborted.
- Decision: invert the two write operations to consume a stream and return the resulting total size:
  ```csharp
  Task<long>   WriteAsync(record, Stream content, ct);
  Task<long>   AppendAsync(record, Stream content, ct);
  Task<Stream> ReadAsync(record, ct);   // unchanged
  ```
  Reading keeps handing a stream out. The asymmetry is deliberate and mirrors every storage SDK — `GetObject` returns a stream, `PutObject` accepts one; a consumer reads at its own pace while a producer hands over its source.
- Consequences:
  - The write is complete when the call returns. No disposal order to get wrong, no window where the content does not exist, and the size is known exactly once at the moment the bytes land — hence the `long` return.
  - `#003` was the right call but the problem it managed is largely gone: with no returned write stream there is nothing to mis-order. Its rule still applies to `ReadAsync`.
  - `TrackSizeOnDispose` (#002) **stays**. `FileSystemBlobDataStore` now records `record.Size` as it writes, but `BlobRecord.Size` has an `internal` setter, so an `IBlobDataStore` implemented in another assembly cannot. The dispose-time refresh is what keeps the persisted column right for any backend.
  - Opens the way to computing `Hash` while the bytes stream through, which #002 left undone because there was no single place that saw them.
  - Cost: a producer whose API only writes (`JsonSerializer.SerializeAsync`, `XmlWriter`, `GZipStream` compress) needs a `System.IO.Pipelines` bridge. This is ergonomics only — back-pressure and memory behaviour are unaffected, since the store pulls at the source's rate. That bridge ships as a producer-delegate overload of `WriteAsync`/`AppendAsync`. **Amended the same day**: it started as extension methods but became **default interface methods** on `IBlobDataStore`, because an extension is static and a backend could not specialise it — worse, a call through `IBlobDataStore` would silently pick the extension over a same-named instance method. `ProducerStreamBridge` (internal) holds the pipe default; `FileSystemBlobDataStore` overrides both and hands its own `FileStream` over, since bridging buys nothing when the store already owns a writable destination. Consequence: the supplied stream's seekability now varies by store, so the contract promises only that it is write-only, and `WriteThroughAsync` reports `file.Length` rather than `file.Position` because a producer holding the real file stream may seek.
  - Rejected: also putting `OpenWriteAsync` on `IBlobDataStore` (Azure ships both directions). It would reintroduce the "which write method?" fork that #005 removed. Add it only for a concrete large write-only producer on a backend with a native push API, where routing through our pipe would be pure overhead.
