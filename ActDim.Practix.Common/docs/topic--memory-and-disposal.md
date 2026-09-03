---
protocol: along
protocol_version: "2.2.18"
slug: memory-and-disposal
title: Memory Management & Disposal Lifecycle
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [memory, buffer, array-pool, disposal, reachability]
---

# Memory Management & Disposal Lifecycle

`ActDim.Practix.Common` provides zero-allocation buffer pooling abstractions, recyclable stream management, atomic disposal actions, and GC reachability observers to prevent memory leaks and Large Object Heap (LOH) fragmentation.

---

## Buffer Ownership (`IBufferOwner<T>` & `ArrayPoolBufferOwner<T>`)

`IBufferOwner<T>` defines an owned memory buffer lease backed by an array pool or native memory:

```csharp
using IBufferOwner<byte> owner = ArrayPoolBufferOwner<byte>.Rent(4096);

byte[] array = owner.Array;       // Underlying pooled array
Memory<byte> memory = owner.Memory; // Valid memory slice (length 4096)
int length = owner.Length;        // 4096

// On dispose: underlying array is returned to ArrayPool<byte>.Shared
```

### Key Contract:
- `Array`: Accessing `Array` after disposal throws `ObjectDisposedException`.
- `Dispose()`: Interlocked exchange guarantees that the rented array is returned to the pool exactly once.

---

## Shared Stream Pooling (`MemoryManager`)

`MemoryManager.Default` exposes a pre-configured `RecyclableMemoryStreamManager` with parameters tuned for high throughput:

```csharp
// Rent pooled seekable stream
using MemoryStream stream = MemoryManager.Default.GetStream("MyOperation");

await source.CopyToAsync(stream);
stream.Position = 0;
```

### Configuration Tuning:
- **Block Size**: 8 KB blocks.
- **Large Buffer Multiple**: 1 MB increments.
- **Max Buffer Size**: 16 MB.
- **Free Pools**: 64 MB small pool / 256 MB large pool.
- **ThrowExceptionOnToArray**: `true` (prohibits calling `.ToArray()`, preventing accidental heap duplication).

---

## Atomic Disposal Primitives (`DisposableAction` & `DisposableAsyncAction`)

Encapsulates cleanup logic in an `IDisposable` or `IAsyncDisposable` handle that executes **at most once**:

```csharp
// Synchronous atomic action
IDisposable scope = new DisposableAction(() => ReleaseLock());
scope.Dispose(); // Executes ReleaseLock()
scope.Dispose(); // Idempotent no-op

// State-carrying non-allocating action (avoids delegate closure allocation)
IDisposable tokenScope = new DisposableAction<string>(
    key => ReleaseKey(key),
    "lock_resource_1"
);

// Asynchronous atomic action
IAsyncDisposable asyncScope = new DisposableAsyncAction(async () =>
{
    await client.DisconnectAsync();
});
```

---

## Disposer Utility (`Disposer`)

Safe, null-tolerant disposal helper that cleans up objects, collections, and tuples without throwing null reference exceptions:

```csharp
Disposer.Dispose(stream);
Disposer.Dispose(listOfDisposables);
```

---

## GC Reachability Observer (`ReachabilityObserver<T>`)

Tracks when an unmanaged or weak object becomes unreachable by the Garbage Collector:

```csharp
ReachabilityObserver<MyResource>.Subscribe(resource, () =>
{
    Console.WriteLine("Resource has been collected by GC");
});
```

- **Mechanism**: Attaches a lightweight finalizer node via `ConditionalWeakTable<T, Observer>`.
- **Constraint**: The subscription callback must **not** capture or reference the target instance (doing so would keep the object reachable and prevent GC collection).

