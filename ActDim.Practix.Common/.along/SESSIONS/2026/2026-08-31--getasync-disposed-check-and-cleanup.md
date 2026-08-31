---
protocol: along
date: 2026-08-31
slug: getasync-disposed-check-and-cleanup
agent: antigravity
branch: main
commit: pending
summary: Added ThrowIfDisposed guard to GetAsync and post-factory cleanup of objects created during concurrent pool disposal.
issues_advanced: []
issues_completed: [bug--getasync-disposed-check-and-cleanup]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: AsyncObjectPool GetAsync Disposed Checks and Cleanup

## Summary of Changes
1. **`AsyncObjectPool<T>.GetAsync`**:
   - Added `ThrowIfDisposed()` helper to immediately throw `ObjectDisposedException` when pool is already disposed.
   - Added check immediately after `await _factory()` to detect if pool was disposed while the factory was executing; if disposed, the freshly created object is immediately disposed via `DisposeItemAsync(item)` and `ObjectDisposedException` is thrown.
   - Guarded `_semaphore.Release()` in `GetAsync` catch block and `DiscardAsync` to only release if `_disposed == 0`.
2. **Tests**:
   - Added `GetAsync_ThrowsObjectDisposedException_WhenPoolIsDisposed` and `GetAsync_CleansUpFactoryCreatedItem_WhenPoolDisposedDuringFactoryExecution` in `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`.
   - All 248 tests in `Common.Tests` passing cleanly.

