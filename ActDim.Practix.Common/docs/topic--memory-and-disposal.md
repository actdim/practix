---
protocol: along
protocol_version: "2.2.18"
slug: memory-and-disposal
title: Memory Management & Disposal Lifecycle
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [memory, buffer, array-pool, buffer-owner, disposal, recyclable-stream, reachability]
---

# Memory Management & Disposal Lifecycle

`ActDim.Practix.Common` provides high-performance memory pooling abstractions, deterministic buffer ownership models, recyclable stream managers, atomic disposal actions, and GC reachability observers.

---

## Buffer Ownership Model (`IBufferOwner<T>` & `ArrayPoolBufferOwner<T>`)

In high-throughput .NET applications, renting arrays directly from `ArrayPool<T>.Shared` often leads to bugs where developers forget to return arrays, return arrays multiple times, or accidentally read past the logical data length (since rented arrays have bucketing power-of-two lengths).

`IBufferOwner<T>` solves these issues by encapsulating leased memory in a deterministic, disposable handle:

```csharp
public interface IBufferOwner<T> : IDisposable
{
    T[] Array { get; }
    Memory<T> Memory { get; }
    int Length { get; }
}
```

### 1. `ArrayPoolBufferOwner<T>` (Pooled Array Wrapper)

Rents an array of at least `size` from an `ArrayPool<T>` (defaulting to `ArrayPool<T>.Shared`):

```csharp
using (IBufferOwner<byte> buffer = ArrayPoolBufferOwner<byte>.Rent(1024))
{
    byte[] rawArray = buffer.Array;        // Rented bucket array (e.g. Length = 1024 or 2048)
    int validLength = buffer.Length;        // Exactly 1024 (the requested length)
    Memory<byte> slice = buffer.Memory;     // Slice: rawArray.AsMemory(0, 1024)

    // Pass buffer.Memory to socket, serializer, or crypto routines
    await socket.ReceiveAsync(buffer.Memory, SocketFlags.None);
} // Dispose returns the rented array back to ArrayPool<T>.Shared via Interlocked.Exchange
```

### Key Guarantees:
- **Exact Logical Slicing**: `buffer.Memory` is automatically sliced to `[0..Length]`, preventing callers from inspecting dirty uninitialized bytes in the remainder of the pool bucket.
- **Double-Return & Use-After-Free Protection**: `Dispose()` uses `Interlocked.Exchange(ref _array, null)`. Calling `Dispose()` multiple times is an idempotent no-op. Attempting to access `.Array` or `.Memory` after disposal throws `ObjectDisposedException`.

### 2. `ArrayBufferOwner<T>` (Unpooled / Managed Array Wrapper)

Wraps an existing managed array in the `IBufferOwner<T>` contract when pooling is not required (e.g., small arrays or pre-allocated constants):

```csharp
byte[] preallocated = new byte[64];
using IBufferOwner<byte> owner = new ArrayBufferOwner<byte>(preallocated);
```

---

## Process-Wide Recyclable Stream Pooling (`MemoryManager.Default`)

`MemoryManager.Default` exposes a singleton `RecyclableMemoryStreamManager` configured for server workloads, eliminating Large Object Heap (LOH) fragmentation caused by `MemoryStream` expansions:

```csharp
using MemoryStream stream = MemoryManager.Default.GetStream("NetworkProcessor");

await inputPayload.CopyToAsync(stream);
stream.Position = 0;
```

### High-Performance Configuration Matrix:

```csharp
var blockSize = 8 * 1024;               // 8 KB small blocks
var largeBufferMultiple = 1024 * 1024;    // 1 MB increments for large blocks
var maxBufferSize = 16 * 1024 * 1024;     // 16 MB maximum single buffer size
var maximumFreeSmallPoolBytes = 64 * 1024 * 1024;   // 64 MB small pool capacity
var maximumFreeLargePoolBytes = 256 * 1024 * 1024;  // 256 MB large pool capacity
```

### Critical Invariants:
1. **`ThrowExceptionOnToArray = true`**:
   - Calling `.ToArray()` on a `RecyclableMemoryStream` allocates a contiguous byte array on the heap, completely defeating the purpose of memory pooling.
   - `MemoryManager.Default` enables `ThrowExceptionOnToArray`, forcing developers to use zero-copy APIs (`GetBuffer()`, `TryGetBuffer()`, `ZeroAllocCopyTo()`, `ReadBytes()`).
2. **`AggressiveBufferReturn = true`**:
   - Stream segments and large buffers are immediately returned to pool buckets upon `stream.Dispose()`.

---

## Atomic Disposal Primitives (`DisposableAction` & `DisposableAsyncAction`)

Encapsulates cleanup logic in thread-safe `IDisposable` and `IAsyncDisposable` wrappers that guarantee single execution:

```csharp
// 1. Synchronous atomic action
IDisposable releaseLock = new DisposableAction(() => Mutex.ReleaseMutex());
releaseLock.Dispose(); // Executes callback
releaseLock.Dispose(); // Safe no-op

// 2. Non-allocating parameterized action (no closure allocation)
IDisposable scopedToken = new DisposableAction<string>(
    state => RemoveLock(state), 
    "resource_key_42"
);

// 3. Asynchronous atomic action
IAsyncDisposable asyncCleanup = new DisposableAsyncAction(async () =>
{
    await connection.CloseAsync();
});
await asyncCleanup.DisposeAsync();
```

---

## Object Reachability Observer (`ReachabilityObserver<T>`)

Monitors when an unmanaged or weak reference target becomes unreachable by the Garbage Collector without modifying the target class:

```csharp
var tracker = new LargeResource();

ReachabilityObserver<LargeResource>.Subscribe(tracker, () =>
{
    Console.WriteLine("LargeResource was garbage collected");
});
```

### Rules & Internal Operation:
- Attaches a lightweight finalizer node via `ConditionalWeakTable<T, Observer>`.
- **Constraint**: The subscription callback delegate must **never capture or reference** the observed object instance (such a reference would form a strong root and prevent the object from ever being collected).
