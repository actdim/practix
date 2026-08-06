# delete-blob-content

- status: done
- created: 2026-08-05
- updated: 2026-08-05

## Problem

Every deletion path removes metadata only. `IBlobDataStore` has no delete operation at all, so
the stored bytes are never touched:

- `DeleteAsync(key)` — drops the `blob_records` row and the locks, leaves the file.
- `DeleteExpiredAsync` / `DeleteOlderThanAsync` / `CleanupAsync` — same, in bulk.

Consequences: storage grows without bound (worst for TTL/sliding-expiration cache usage, where
records expire constantly), and recreating a deleted key reports `IsNew` while stale content sits
on disk.

## Design

`IBlobDataStore.DeleteAsync(record, ct)` requires a write lock, like every other mutating
operation, and returns whether anything was removed. `BlobManager` orchestrates every deletion,
since it is the only layer that sees both the registry and the data store.

Ordering is **content first, then metadata**: a leftover registry row is recoverable — decision
#001 reports it as `IsNew` and `TryGetFor*` prunes it — whereas a leftover file is invisible to
the library and lost for good.

Single key: acquire the write lock through the registry, delete content, delete rows while still
holding the lock (`IBlobRegistry.DeleteLockedAsync`), release.

Bulk: the registry selects candidate keys (its existing SQL conditions already exclude locked
records), then each key goes through the same single-key routine with a minimal timeout, so a key
that got locked in the meantime is skipped rather than waited on. This keeps the current
"skips locked records" semantics. `forceDeleteLocked` breaks existing locks first via
`IBlobRegistry.ForceUnlockAsync`, then deletes normally.

Trade-off accepted: bulk deletion is no longer a single SQL statement but N lock acquisitions.
That is the price of not deleting content out from under a live reader.

## Done when

- [x] `IBlobDataStore.DeleteAsync` + `FileSystemBlobDataStore` implementation that also prunes the
      shard directories it empties
- [x] all four deletion paths remove content
- [x] tests assert the bytes are gone, not just the record
