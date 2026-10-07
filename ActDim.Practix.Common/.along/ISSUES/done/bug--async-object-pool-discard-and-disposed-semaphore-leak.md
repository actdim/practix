---
protocol: along
slug: async-object-pool-discard-and-disposed-semaphore-leak
type: bug
status: done
completed: 2026-10-07
priority: medium
created: 2026-09-15
updated: 2026-10-07
agent: antigravity
tags: [pooling, asyncobjectpool, semaphore, concurrency, race-condition]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
allowed_roots: [../Tests/Common.Tests]
write_scope: [../Tests/Common.Tests]
---

# AsyncObjectPool Discard Unbounded Capacity Expansion and Disposed Semaphore Slot Leak

## Overview
`ActDim.Practix.Pooling.AsyncObjectPool<T>` contains two concurrency defects: a public unvalidated `DiscardAsync(T)` method that allows arbitrary external objects to skew pool accounting and exceed `maxSize`, and a semaphore slot leak if disposal occurs while awaiting pool item acquisition.

## Identified Defects

### 1. Public Unchecked `DiscardAsync(T)` Corrupts Pool Size
In `AsyncObjectPool.cs` (lines 120-140):
```csharp
public async ValueTask DiscardAsync(T item)
{
    if (item == null)
    {
        return;
    }

    Interlocked.Decrement(ref _createdCount);

    if (Volatile.Read(ref _disposed) == 0)
    {
        try
        {
            _semaphore.Release();
        }
        catch (ObjectDisposedException) { }
    }
...
```
Because `DiscardAsync` is public and accepts any instance of `T`, calling `pool.DiscardAsync(foreignInstance)` decrements `_createdCount` and calls `_semaphore.Release()`. This artificially inflates the semaphore count, permitting the pool to instantiate more objects than `maxSize`, violating its maximum capacity constraint.

### 2. Semaphore Slot Leak When Disposed in `GetAsync`
In `AsyncObjectPool.cs` (lines 78-83):
```csharp
await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

if (Volatile.Read(ref _disposed) != 0)
{
    throw new ObjectDisposedException(nameof(AsyncObjectPool<T>));
}
```
If the pool is disposed after or while waiting for `_semaphore.WaitAsync`, a semaphore slot has already been acquired. The method throws `ObjectDisposedException` immediately without releasing the semaphore slot. If other threads or cleanup routines depend on the semaphore state, this creates an unreleased slot inconsistency.

### 3. Pending `GetAsync` Waiters Hang Forever on `DisposeAsync` (code review finding)
`DisposeAsync` called `_semaphore.Dispose()`. `SemaphoreSlim.Dispose()` does not complete pending `WaitAsync` tasks, so every caller parked in `GetAsync` when the pool was disposed (e.g. evicted from the cache) never returned.

The previous `Channel<T>`-based implementation (MLWeb `MapServer/AsyncObjectPool.cs`) is not a valid fallback: there a discard / factory failure frees capacity only in a counter and never wakes callers blocked in `ReadAsync`; fixing that with "free slot" sentinels in the same FIFO breaks lazy reuse-first semantics.

## Resolution: all defects resolved (2026-10-07)
- Defect 1: `DiscardAsync(T item)` visibility restricted from `public` to `internal`. External callers discard items strictly via `PooledObject.DiscardAsync()`, which exchanges the owned item handle and guarantees that foreign instances cannot skew pool capacity accounting.
- Defects 2 and 3: Semaphore created as `SemaphoreSlim(maxSize)` (no max count) and never disposed; all `catch (ObjectDisposedException)` around `Release` removed.
- Invariant: every acquired permit is released exactly once on every path (return, discard, factory failure, disposal during factory, post-disposal wake-up).
- "Baton" wake-up: `DisposeAsync` releases one permit; each awakened waiter sees `_disposed`, releases one permit for the next waiter and throws `ObjectDisposedException`. No per-call `CancellationTokenSource`, no waiter counter.
- Tests: `DisposeAsync_WakesAllPendingWaiters_WithObjectDisposedException`, `ReturnAfterDisposeAsync_InvokesDisposer`, `DiscardAsync_WakesPendingWaiter_WithFreshInstance` (in `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`).
- Docs: `docs/topic--async-object-pool.md` (baton section and invariants).


