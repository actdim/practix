---
protocol: along
protocol_version: "4.4.1"
slug: read-lock-lost-content
type: bug
status: done
completed: 2026-10-02
priority: high
created: 2026-10-02
updated: 2026-10-02
agent: antigravity
tags: [concurrency, cache, locking, pkg-bytepath]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Reconcile read lock on lost content

- status: done
- created: 2026-10-02
- completed: 2026-10-02

## Problem

When a record exists in the registry but its content file was deleted or lost from the data store, calling `TryGetOrSetAsync` with `LockType.Read` causes the registry to downgrade the write lock to a read lock because the registry perceives the record as existing (`!isNew`).

Then `ReconcileContentAsync` notices `size == null` and, because `allowNew == true`, sets `blobResult.IsNew = true` while keeping the lock held untouched (`LockType.Read`).

The caller receives `IsNew = true`, indicating it must produce and write the content. However, calling `WriteAsync` or `ProduceIntoAsync` on the data store throws `InvalidOperationException` via `EnsureWriteLock` because the record is held under a read lock. Consequently, the cache entry cannot be repopulated and self-healing fails.

## Acceptance Criteria

- [x] `BlobManager.ReconcileContentAsync` re-acquires a write lock when `size == null && allowNew && LockType != LockType.Write`.
- [x] Automated tests passing: verify that `TryGetOrSetAsync` with `LockType.Read` on an existing key with lost content returns a record under `LockType.Write` with `IsNew = true`, and writing content succeeds without throwing.
