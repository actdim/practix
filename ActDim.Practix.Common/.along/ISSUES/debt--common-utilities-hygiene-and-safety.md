---
protocol: along
slug: common-utilities-hygiene-and-safety
type: debt
status: open
priority: low
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [hygiene, security, memory, reachability, finalizer, disposer]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Common Utilities Hygiene, Memory Boundaries, and Finalizer Safety

## Overview
Review of miscellaneous utility components in `ActDim.Practix.Common` (`RandomId`, `MemoryManager.Default`, `ReachabilityObserver`, `Disposer`) identified misleading documentation, unbounded process-wide memory retention, hazardous finalizer thread execution, and partial collection disposal.

## Identified Defects

### 1. `RandomId` Inaccurate Collision-Resistant Claims
In `RandomId.cs`:
The class XML documentation characterizes generated IDs as "collision-resistant". In reality, it generates a fixed-length string using CSPRNG bytes. While cryptographically random, its collision probability is mathematically bound to its fixed bit-entropy and Birthday Paradox constraints, rather than cryptographic collision resistance in the hashing sense. The documentation should accurately describe it as cryptographically random rather than collision-resistant.

### 2. `MemoryManager.Default` Unbounded Retention Limits
In `ActDim.Practix.Common/Memory/MemoryManager.cs`:
The default process-wide `RecyclableMemoryStreamManager` allocates large contiguous block pools with maximum stream sizes up to 1 GB and hundreds of megabytes in free block pools. In multi-tenant or memory-constrained container environments, this default retains substantial uncollected memory without proactive trim or eviction thresholds.

### 3. `ReachabilityObserver` Executes User Code on Finalizer Thread
In `Runtime/ReachabilityObserver.cs` (lines 68-71):
```csharp
~Observer()
{
    _handler?.Invoke();
}
```
The finalizer `~Observer()` synchronously invokes the registered user delegate `_handler`. Invoking arbitrary user code directly on the CLR finalizer thread is hazardous: any unhandled exception crashes the process immediately, and long-running or blocking operations block the single global finalizer queue, preventing garbage collection of other finalized objects.

### 4. `Disposer.DisposeCollection` Aborts on First Exception and Skips Collection
In `Disposal/Disposer.cs` (lines 29-44):
```csharp
public static void DisposeCollection<T>(ref T collectionOfDisposables)
    where T: class, IEnumerable<IDisposable>
{
    var collection = collectionOfDisposables;
    if (collection == null) return;

    collectionOfDisposables = null;
    foreach (var item in collection)
    {
        item.Dispose();
    }
}
```
If an item's `item.Dispose()` throws an exception, the loop terminates immediately and all subsequent items in the collection remain un-disposed. Additionally, if the collection itself implements `IDisposable`, it is never disposed.

## Remediation Plan
1. Clarify XML documentation for `RandomId`.
2. Provide configurable memory thresholds for `MemoryManager.Default`.
3. Dispatch `ReachabilityObserver` callbacks asynchronously to the thread pool (`ThreadPool.UnsafeQueueUserWorkItem` or `Task.Run`) instead of running on the finalizer thread, and protect against unhandled exceptions.
4. Update `Disposer.DisposeCollection` to use aggregate exception collection and dispose the collection container if it implements `IDisposable`.

