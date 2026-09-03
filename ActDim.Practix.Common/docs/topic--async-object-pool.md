---
protocol: along
protocol_version: "2.2.18"
slug: async-object-pool
title: Asynchronous Bounded Object Pool
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [pooling, async, concurrency, object-pool, fault-tolerance]
---

# Asynchronous Bounded Object Pool

`AsyncObjectPool<T>` provides a thread-safe, bounded, FIFO-ordered asynchronous object pool. It limits the total number of instantiated objects and orchestrates concurrent consumer leases without blocking OS threads.

---

## Architecture & Concurrency Model

- **Bounded Capacity**: Total active instances (idle in queue + leased by callers) cannot exceed `maxSize`.
- **Concurrency Control**: Coordinated via `SemaphoreSlim(maxSize, maxSize)` and `ConcurrentQueue<T>`.
- **FIFO Ordering**: Idle instances are reused in First-In, First-Out order to guarantee uniform reuse and reduce cache eviction spikes.
- **Asynchronous Waiting**: When all instances are leased, `GetAsync` asynchronously awaits a free slot without thread starvation.

---

## Leased Handle Lifecycle & `PooledObject`

Calling `GetAsync` returns a `PooledObject` handle implementing `IAsyncDisposable`:

```csharp
var pool = new AsyncObjectPool<DbConnection>(
    factory: () => CreateOpenConnectionAsync(),
    maxSize: 16,
    disposer: async conn => await conn.DisposeAsync()
);

// Standard lease pattern
await using (var handle = await pool.GetAsync(cancellationToken))
{
    var connection = handle.Item;
    await connection.ExecuteQueryAsync("SELECT 1");
} // On dispose: handle.Item is safely returned to the pool FIFO queue
```

---

## Discarding Corrupted Instances (`DiscardAsync`)

If an object encounters a terminal failure (such as broken socket, connection timeout, corrupted stream state), returning it to the pool would poison subsequent callers.

`handle.DiscardAsync()` or `pool.DiscardAsync(item)`:
1. Atomically unbinds the instance from the lease handle.
2. Decrements `_createdCount`.
3. Invokes the configured `disposer` delegate for clean resource release.
4. Releases a `SemaphoreSlim` permit so the pool can instantiate a fresh object on demand.

```csharp
await using var handle = await pool.GetAsync(ct);
try
{
    await handle.Item.SendNetworkPayloadAsync(data);
}
catch (SocketException)
{
    // Discard corrupted socket: releases semaphore slot and invokes disposer
    await handle.DiscardAsync();
    throw;
}
```

---

## Eviction & Fault-Tolerant Draining (`DisposeAsync`)

When the pool itself is disposed (such as upon application shutdown or cache eviction):
- The pool marks itself disposed (`_disposed = 1`).
- All remaining parked items in `_items` are dequeued and processed through `disposer`.
- If any disposer throws an exception, all exceptions are collected and rethrown collectively in an `AggregateException`, ensuring complete draining even in the presence of failures.
- Objects returned to an already-disposed pool are immediately disposed via `DisposeItemAsync` rather than requeued.

---

## Key Invariants

| Scenario | Behavior |
| :--- | :--- |
| **Object Disposal** | Ownership is explicit: the pool only disposes objects if an explicit `disposer` delegate was passed to constructor. |
| **GetAsync on Disposed Pool** | Throws `ObjectDisposedException`. If factory was already running, the created instance is immediately cleaned up. |
| **Factory Failure** | If the factory delegate fails or throws, the reserved semaphore slot is released immediately to prevent capacity leaks. |
| **Discard on Null** | `DiscardAsync(null)` is a safe no-op. |

