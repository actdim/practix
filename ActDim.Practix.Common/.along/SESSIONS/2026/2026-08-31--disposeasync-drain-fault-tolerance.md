---
protocol: along
date: 2026-08-31
slug: disposeasync-drain-fault-tolerance
agent: antigravity
branch: main
commit: pending
summary: Implemented fault-tolerant idle object draining in AsyncObjectPool.DisposeAsync, collecting exceptions and throwing AggregateException.
issues_advanced: []
issues_completed: [bug--disposeasync-drain-fault-tolerance]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: AsyncObjectPool.DisposeAsync Fault-Tolerant Draining

## Summary of Changes
1. **`AsyncObjectPool<T>.DisposeAsync`**:
   - Wrapped `await DisposeItemAsync(item)` in `try/catch (Exception ex)` inside the idle draining loop (`while (_items.TryDequeue(out var item))`).
   - Exceptions are collected into a `List<Exception>`, ensuring that a failure when disposing one item does not terminate the loop and prevent other idle objects from being disposed.
   - Guaranteed execution of `_semaphore.Dispose()`.
   - Throws `AggregateException` at the end if any item disposers threw exceptions.
2. **Tests**:
   - Added unit test `DisposeAsync_DrainsAllIdleObjects_EvenWhenDisposerThrows_AndThrowsAggregateException` in `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`.
   - All 246 tests in `Common.Tests` passing.

