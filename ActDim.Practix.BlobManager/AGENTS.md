<!-- BEGIN ACTDIM-AGENTS-PROTOCOL ref=../AGENTS.md (managed by init-agents — do not edit by hand) -->
This folder belongs to a repository that uses the ACTDIM-AGENTS structure. The full working
guidance + agent-context protocol live once in the nearest ancestor `AGENTS.md` (`../AGENTS.md`) —
read it there. This folder keeps its OWN `.agents/` state; use the nearest one.
Only this folder's specifics follow.
<!-- END ACTDIM-AGENTS-PROTOCOL -->

## Project specifics

# ActDim.Practix.BlobManager

Blob storage split into two layers that know nothing about each other:

- **metadata + distributed locks** — SQLite (`blob_records`, `blob_locks`)
- **content** — the file system (`FileSystemBlobDataStore`)

`BlobManager` is the only place that sees both, which is why every operation spanning the two lives
there.

---

## Layout

```
IBlobManager.cs             # public API
IBlobDataStore.cs          # public content contract
BlobStoreOptions.cs        # public options (file is named IBlobStoreOptions.cs — class, not interface)
LockType.cs                # None / Read / Write
BlobErrorCode.cs           # None / KeyNotFound / Timeout
BlobRecord.cs              # near-POCO + IAsyncDisposable (dispose releases the lock)
BlobResult.cs              # (ErrorCode, Record, IsNew), deconstructable, internal ctor
BlobManager.cs             # internal — orchestrates registry + data store
IBlobRegistry.cs           # internal — metadata + lock contract
SQLiteBlobRegistry.cs      # internal — lock engine + persistence
FileSystemBlobDataStore.cs # public — FS implementation

Tests/BlobManager.Tests/BlobManagerTests.cs   # 41 xUnit v3 tests
```

`README.md` is the consumer-facing document: the API plus the reasoning behind the shapes that look
unusual (why writes consume a stream, why the producer-delegate overload exists, why there is no
`OpenWriteAsync`). Keep it in sync when the public surface changes.

Read `.agents/DECISIONS.md` before changing any of the invariants below — #001–#006 explain why
they are the way they are, including alternatives already rejected.

---

## Core invariants

### 1. Results carry codes, handles carry locks

Every acquiring method returns `BlobResult`, deconstructable as `(BlobErrorCode, BlobRecord)`:

| `BlobErrorCode` | meaning |
|---|---|
| `None` | success — `Record` is non-null and holds a lock |
| `KeyNotFound` | no record, or its content is gone and the caller asked for existing content |
| `Timeout` | lock acquisition timed out, or an orphan could not be established as such |

Check `ErrorCode == None` before touching `Record`. `BlobResult` and `BlobRecord` are both
`IAsyncDisposable`; **always `await using`** — the sync `Dispose()` only fires `OnDispose`.

Read lock: concurrent readers, no writers. Write lock: exclusive. Acquisition is **not re-entrant**
— acquiring a lock you already hold spins until timeout.

### 2. A record without content is transient, and only `TryGetOrSetAsync` may observe it (#001)

`BlobManager.ReconcileContentAsync` runs on every entry point and reconciles the registry with the
data store — it mutates both, hence the name:

- `TryGetOrSetAsync` → `allowNew: true`. No content ⇒ `BlobResult.IsNew = true`, keeping the lock.
  `IsNew` therefore means "there is no content yet", covering both a fresh record and one that
  outlived its blob.
- `TryGetForReadingAsync` / `TryGetForWritingAsync` → `allowNew: false`. No content ⇒ the orphaned
  record is deleted and `KeyNotFound` returned. An `IsNew` result here is an impossible state and
  throws `InvalidOperationException`.

So: write the content before relying on `TryGetFor*`. Tests seed both via `TestEnvironment.SeedAsync`.

### 3. `Size` is owned by the library and read from the data store (#002)

`ReconcileContentAsync` assigns `record.Size` from `GetSizeAsync` on every hand-out; the registry
column is never trusted as the source. Because the handle holds a lock until dispose, that value is
authoritative for its lifetime. `TrackSizeOnDispose` re-reads it for write locks so the persisted
column is correct for readers that never take a handle (`QueryAsync`, reporting).

The setter is `internal` — the library observes the size, callers do not declare it. `Hash` is the
opposite: caller-declared through `BlobStoreOptions`, because computing it means reading the whole
blob. **It is never computed automatically.**

### 4. Stream lifetime is the caller's business (#003)

Dispose the write stream **before** the record. Both idiomatic forms already do this — a nested
`await using` block, or `using` declarations in one scope (reverse declaration order). Disposing the
record first releases the distributed lock while you are still writing, which is a correctness bug
regardless of `Size`.

Do **not** make `BlobRecord` own its streams to enforce this. It was considered and rejected.

### 5. Deletion removes content before metadata (#004)

`BlobManager` owns all four deletion paths. Content first: a leftover registry row is recoverable
via #001, a leftover file is invisible to the library.

The registry deliberately has **no** self-locking delete — `DeleteLockedAsync(record)` requires the
caller to already hold the write lock (attested by `record.LockType`), because acquiring it inside
would self-deadlock. Bulk deletion selects candidates (`GetExpiredKeysAsync`,
`GetKeysOlderThanAsync`), then runs each key through the single-key routine with `TimeSpan.Zero`, so
anything locked since selection is skipped. `forceDeleteLocked` breaks locks first via
`ForceUnlockAsync`.

Lock-acquisition timeouts: `TimeSpan.Zero` means "attempt once, do not wait" — the deadline is
already reached when the first attempt returns. A **negative** value means unspecified and falls back
to the registry default. The lock's own TTL is separate and never below 1 s.

`DeleteAsync` signals through exceptions (`KeyNotFoundException`, `TimeoutException`) since it
returns `Task` — unlike the `TryGet*` family.

### 6. Writes consume a stream, reads hand one out (#005, #006)

```csharp
Task<long>   WriteAsync(record, Stream content, ct);   // FileMode.Create — create or truncate
Task<long>   AppendAsync(record, Stream content, ct);  // FileMode.Append — creates when absent
Task<Stream> ReadAsync(record, ct);                    // FileMode.Open — seekable
Task<bool>   DeleteAsync(record, ct);                  // also prunes emptied shard directories
Task<long?>  GetSizeAsync(record, ct);                 // null ⇒ no content — the single primitive
Task<bool>   ExistsAsync(record, ct);                  // default: GetSizeAsync(...).HasValue
Task<string> ResolveLocationAsync(record, ct);
```

The asymmetry is deliberate and matches every storage SDK: a consumer reads at its own pace, a
producer hands over its source. Because the store **consumes** the content stream, the write is
complete when the call returns — there is no stream to commit later, so no disposal order to get
wrong and no window in which the content does not yet exist. Both return the **resulting total
size**, which is why `AppendAsync` reports the new total rather than the appended length.

The store is pulled at whatever rate the source produces, so an incremental producer needs no
buffering.

For a producer whose API only writes (`JsonSerializer.SerializeAsync`, `XmlWriter`, `GZipStream` in
compress mode), `IBlobDataStore` carries a producer-delegate overload:

```csharp
await manager.DataStore.WriteAsync(record, (stream, token) =>
    JsonSerializer.SerializeAsync(stream, dto, cancellationToken: token), ct);
```

Completion is not the caller's problem: returning from the delegate is the signal, so no stream
outlives the call. The guarantee is structural, not type-level.

These are **default interface methods**, so a backend gets push-style writing for free and can
specialise it. `ProducerStreamBridge` holds the default — a `System.IO.Pipelines` bridge, used by any
store that can only consume content; a producer failure travels through the pipe, so the caller sees
the original exception. `FileSystemBlobDataStore` **overrides** both and hands its `FileStream` over
directly, since bridging would only add a copy and a second task.

Because the two differ, the supplied stream is write-only and callers must **not assume it is
seekable**. That is also why `WriteThroughAsync` reports `file.Length` rather than `file.Position` — a
producer holding the real file stream may seek within it.

There is deliberately no `OpenWriteAsync` on `IBlobDataStore`: handing a stream back to the caller
returns the completion contract this design removed, and it would reintroduce the "which write method?"
fork #005 killed.

`ExistsAsync` is a **default interface implementation** derived from `GetSizeAsync`, so a store has
one primitive to implement and the two can never disagree. Override it only for a backend with a
genuinely cheaper existence probe. Being a default implementation it is reachable through
`IBlobDataStore` (which is how `manager.DataStore` is typed), not through a concrete class.

`null` means no content; a size of `0` is a real, existing zero-byte blob. Conflating them would
make `TryGetFor*` delete the record of a legitimately empty blob.

`WriteAsync` is correct for a new and an existing key alike — never consult `IsNew` to pick a write
method. `AppendAsync` takes no offset: the store knows the size. Resumable upload works through
`record.Size` plus a plain append; patching the middle of a blob is not supported.

`ReadAsync` **promises a seekable stream**, so a range is read by seeking and resumable download needs
no extra method. A backend whose native stream is forward-only (S3's `GetObject`) must wrap it;
Azure's `OpenReadAsync` already behaves this way.

`FileSystemBlobDataStore` validates the lock before I/O: reads need `Read` or `Write`, everything
else needs `Write`. A record with the wrong lock throws `InvalidOperationException`.

### 7. Options are instructions, the record is state (#007)

Setter visibility follows what a value **is**, not whether mutating it is safe — a write lock is
exclusive, so mutation under one is exactly what the lock is for. Facts the library observes are
`internal` (`Size`, `Hash`, `CreatedAt`/`UpdatedAt`/`AccessedAt`, `Key`); intent only the caller can
supply stays public (`ContentType`, `Metadata`, `SlidingExpiration`, `ExpiresAt`).

`BlobRecord.Apply(BlobStoreOptions)` applies instructions to a record already held under a write lock —
`Ttl` resolved against now, only values that were set, expiration priority. The registry uses an
`internal Apply(options, now)` overload because it applies options before `LockType` is decided and
needs its own `now`. Do not move this back into the registry: it is computation over a record, not
persistence.

Known hole: `UpdateOnReadDisposeAsync` persists the record too, so mutations under a **read** lock
still reach storage. The public `Apply` guards; the plain setters do not.

### 8. Expiration

Priority in `ApplyOptions`: `AbsoluteExpiration` > `Ttl` > existing `SlidingExpiration`. Setting
`SlidingExpiration` also persists `sliding_expiration_seconds` so it is re-applied on each access
(read dispose refreshes `AccessedAt`, write dispose also `UpdatedAt`).

`lockType` on `TryGetOrSetAsync` is honoured only for an existing record: new ⇒ always `Write`;
existing + `Read` ⇒ metadata saved under a write lock, then downgraded. That downgrade releases and
re-acquires, which is safe only because nothing is being destroyed — see #004 for why the same
pattern is forbidden in deletion.

---

## Storage details

**`blob_records`** — `blob_key` TEXT PK, `metadata`, `content_type`, `size`, `hash`, `created_at`,
`updated_at`, `accessed_at`, `sliding_expiration_seconds`, `expires_at`. Index on `expires_at`.

**`blob_locks`** — `blob_key` (FK → `blob_records` ON DELETE CASCADE), `is_write_lock`, `locked_by`
(UUID), `locked_at`, `expires_at`. Index on `blob_key`.

All timestamps are Unix seconds. `expires_at` uses **ceiling** rounding in both tables so integer
truncation cannot expire something early. Minimum lock TTL is 1 s.

Locks are acquired with `BEGIN IMMEDIATE` + conditional `INSERT ... WHERE NOT EXISTS` + `changes()`,
retried every 100 ms until the timeout. Stale locks are pruned at acquisition time. All DB access is
serialized through `SemaphoreSlim(1,1)` — `SQLiteOpenFlags.FullMutex` is **not** a substitute.

**Path layout** — `BuildPath` splits the key on `/` and `\`: a key with separators becomes
subfolders plus a file name; a flat key gets two shard directories from the first 4 hex chars of its
`XxHash3`, so blobs do not pile into one directory. That `XxHash3` hashes the **key**, not the
content. Invalid file-name chars become `_`; an empty key falls back to `blob`.

---

## Usage

```csharp
var registry  = new SQLiteBlobRegistry(dbPath, TimeSpan.FromSeconds(30));
var dataStore = new FileSystemBlobDataStore(filesPath);
IBlobManager manager = new BlobManager(dataStore, registry);

var (ec, record) = await manager.TryGetOrSetAsync("my-key", new BlobStoreOptions { Ttl = TimeSpan.FromHours(1) }, LockType.Write, ct);
if (ec == BlobErrorCode.None)
{
    await using (record)
    {
        await using var stream = await manager.DataStore.WriteAsync(record, ct);
        // write bytes — stream closes before the record, releasing the lock last
    }
}
```

---

## Conventions

- Namespace and assembly: `ActDim.Practix.BlobManager`; target `net10.0`; `Nullable` disabled
- Public: `IBlobManager`, `IBlobDataStore`, `BlobStoreOptions`, `BlobRecord`, `BlobResult`,
  `BlobErrorCode`, `LockType`, `FileSystemBlobDataStore`
- Internal: `BlobManager`, `IBlobRegistry`, `SQLiteBlobRegistry`, `BlobRecordTransport`
- `InternalsVisibleTo("ActDim.Practix.BlobManager.Tests")` — tests construct internals directly
- Tests: xUnit v3, `TestContext.Current.CancellationToken`, isolated temp DB + data dir per test via
  `TestEnvironment`, which also exposes `SeedAsync` / `ReadTextAsync` / `LocateAsync` / `Registry`

## Open

- `Hash` is never computed from content (`ComputeXxHash3Async` is commented out)
- `BlobManager` is `internal` with no DI registration helper
- Nullable reference types disabled
- No `IAsyncEnumerable<string>` variant of `QueryAsync`; no tag-based query
- No alternative `IBlobRegistry` (PostgreSQL, Redis) or `IBlobDataStore` (S3, Azure) implementations
  — but `GetSizeAsync` and the two-method write surface were shaped with them in mind
