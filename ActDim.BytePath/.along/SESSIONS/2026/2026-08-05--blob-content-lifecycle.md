---
protocol: along
date: 2026-08-05
slug: blob-content-lifecycle
agent: Claude Code (Opus 5)
branch: main
commit: 4995d1e
summary: >
Reconciled the registry with the data store: records without content are reported via IsNew
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

## Why

The session started from a question about `TryGetOrSetAsync`: is there an explicit "set" that
overwrites a blob by key? It turned out there is (`TryGetOrSetAsync` + `CreateAsync`), but pulling
that thread exposed a chain of related defects around the registry/data-store split. The test suite
was already red at HEAD: 8 of 30 failing: because a previously added existence check deleted
records that carried no content.

## What changed

### Reconciliation of registry and data store (#001)

`VerifyExistsAsync` became `ReconcileContentAsync`, renamed because it mutates both the result and
the registry rather than merely checking. Every entry point runs through it:

- `TryGetOrSetAsync` (4 overloads) passes `allowNew: true`. Missing content sets
  `BlobResult.IsNew = true` on the result whose lock is already held, instead of deleting the
  record and reporting `KeyNotFound`.
- `TryGetFor{Reading,Writing}Async` pass `allowNew: false`: they asked for existing content, so an
  orphaned record is deleted.
- The caller's `timeout` is now actually threaded through (every canary call site passed `null`),
  and a `TimeoutException` from orphan deletion becomes `BlobErrorCode.Timeout` rather than a
  guessed `KeyNotFound`.

The idea came from `CanarySystems.FileStorage` but was reimplemented: the original released the
lock and re-acquired it (a race window), and set `IsNew` on results that could be `Timeout` /
`KeyNotFound`. It also shipped with `allowNew: false` on the two short `TryGetOrSetAsync`
overloads, which makes them throw on every new key: that bug arrived with the port and accounted
for 13 of the 21 failures mid-session.

### `Size` (#002, #003)

`Size` was mapped to and from the `size` column but never computed, so it stayed `NULL` forever.
That blocks appending, which needs the current length. `IBlobDataStore.ExistsAsync` was replaced by
`GetSizeAsync` returning `long?`: existence and size in one round trip, which also suits object
stores where one `HEAD` answers both. `ReconcileContentAsync` assigns it on every hand-out, and
because the record is handed out under a lock held until dispose, the value is authoritative for
the handle's lifetime. `TrackSizeOnDispose` re-reads it for write locks so the persisted column is
right for readers that never take a handle. The setter became `internal`.

Enforcing the "dispose the stream before the record" ordering by having `BlobRecord` own its
streams was considered and **rejected** (#003): the record stays a lightweight near-POCO.

### Deletion removes content (#004, task `delete-blob-content`)

No deletion path touched the stored bytes; `IBlobDataStore` had no delete operation at all.
`BlobManager` now orchestrates all four paths, deleting content before metadata, since a leftover
registry row is recoverable under #001 while a leftover file is invisible. The registry's
key-based `DeleteAsync` / `DeleteExpiredAsync` / `DeleteOlderThanAsync` gave way to
`DeleteLockedAsync(record)`, `ForceUnlockAsync(key)`, `GetExpiredKeysAsync` and
`GetKeysOlderThanAsync`: the old API was a closed atomic operation with no point at which the file
deletion could be interleaved, and re-acquiring the lock inside it would self-deadlock.

### Data-store write surface (#005)

`CreateAsync` removed; `WriteAsync` is `FileMode.Create` so it is correct for new and existing keys
alike. `AppendAsync` lost its `offset` and uses `FileMode.Append`: the store positions the stream
itself. Two rejected alternatives are recorded in #005.

## Files touched

- `IBlobManager.cs`: `DeleteAsync` timeout overload
- `BlobManager.cs`: `ReconcileContentAsync`, `TrackSizeOnDispose`, `DeleteCoreAsync`, `DeleteManyAsync`
- `IBlobRegistry.cs`, `SQLiteBlobRegistry.cs`: deletion decomposed at the lock boundary; key selection
- `IBlobDataStore.cs`, `FileSystemBlobDataStore.cs`: `GetSizeAsync`, `DeleteAsync` + directory pruning, write surface, `BufferSize`
- `BlobRecord.cs`: `Size` setter `internal`
- `BlobResult.cs`: `internal` constructor, `IsNew` settable internally (applied by the user from canary)
- `Tests/BlobManager.Tests/BlobManagerTests.cs`: `TestEnvironment.SeedAsync` / `ReadTextAsync` / `LocateAsync` / `Registry` / `DataPath`
- `AGENTS.md`: rewritten, was describing an API several refactors old

## Decisions

#001 transient content state · #002 `Size` ownership · #003 record/stream decoupling ·
#004 deletion orchestration · #005 write surface

## Tasks

- `delete-blob-content`: done

## Gaps / follow-ups

- `Hash` is still never computed from content; `ComputeXxHash3Async` stays commented out and
  `BlobStoreOptions.Hash` is caller-supplied only.
- `BlobManager` is still `internal` with no DI registration helper.
- Nullable reference types remain disabled in both projects.
- No `IAsyncEnumerable<string>` variant of `QueryAsync`.
- `DeleteAsync` signals through exceptions while the `TryGet*` family uses `BlobErrorCode`;
  consistent within itself but not across the surface.
- The same changes were applied to `CanarySystems.FileStorage`'s `SQLiteBlobRegistry` in that
  repo's own query style. Its `BlobManager` ctor takes a logger provider and its
  `FileSystemBlobDataStore` has a flat `BuildPath` plus an `IFileStorageConfiguration` ctor: those
  divergences must survive when the other files are synced.
