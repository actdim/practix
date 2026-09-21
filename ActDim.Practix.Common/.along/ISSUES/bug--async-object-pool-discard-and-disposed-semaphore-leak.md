---
protocol: along
slug: async-object-pool-discard-and-disposed-semaphore-leak
type: bug
status: open
priority: medium
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [pooling, asyncobjectpool, semaphore, concurrency, race-condition]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
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

## Remediation Plan
1. Validate that the discarded item was indeed an item issued by the pool (e.g. through `PooledObject` handle tracking or an active item registry), or restrict `DiscardAsync` invocation to the `PooledObject` wrapper.
2. In `GetAsync`, wrap post-wait checks in a try block, releasing the semaphore slot if `_disposed != 0` before throwing `ObjectDisposedException`.

