---
protocol: along
slug: async-object-pool
title: Asynchronous Bounded Object Pool
type: topic
created: 2026-09-03
updated: 2026-10-06
tags: [pooling, async, concurrency, object-pool, fault-tolerance]
---

# Asynchronous Bounded Object Pool

`AsyncObjectPool<T>` provides a thread-safe, bounded, FIFO-ordered asynchronous object pool. It limits the total number of instantiated objects and orchestrates concurrent consumer leases without blocking OS threads.

---

## Architecture & Concurrency Model

- **Bounded Capacity**: Total active instances (idle in queue + leased by callers) cannot exceed `maxSize`.
- **Concurrency Control**: Coordinated via `SemaphoreSlim(maxSize)` and `ConcurrentQueue<T>`. Every acquired permit is released exactly once on every path (return, discard, factory failure, disposal). The semaphore is never disposed (see "Waking Pending Waiters on Disposal").
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

### Waking Pending Waiters on Disposal

`SemaphoreSlim.Dispose()` does not complete pending `WaitAsync` calls, so disposing the semaphore would leave callers parked in `GetAsync` hanging forever. The pool therefore never disposes it and uses a "baton" release instead:

1. `DisposeAsync` sets `_disposed` and releases a single permit (the baton) before draining idle items.
2. The awakened waiter observes `_disposed`, releases one permit (passes the baton on) and throws `ObjectDisposedException`.
3. The cascade wakes every pending waiter; callers arriving later find the baton available and fail fast.

This needs no per-call `CancellationTokenSource` and no waiter counter. A waiter cancelled via its token never consumed a permit, so the baton is never lost. The semaphore has no maximum count, so post-disposal releases cannot throw `SemaphoreFullException`. Not disposing it is safe because `SemaphoreSlim` only owns an OS handle once `AvailableWaitHandle` is accessed, which the pool never does.

---

## Key Invariants

| Scenario | Behavior |
| :--- | :--- |
| **Object Disposal** | Ownership is explicit: the pool only disposes objects if an explicit `disposer` delegate was passed to constructor. |
| **GetAsync on Disposed Pool** | Throws `ObjectDisposedException`. If factory was already running, the created instance is immediately cleaned up. |
| **Factory Failure** | If the factory delegate fails or throws, the reserved semaphore slot is released immediately to prevent capacity leaks. |
| **Discard on Null** | `DiscardAsync(null)` is a safe no-op. |
| **Pending Waiters on Disposal** | Every caller awaiting a slot in `GetAsync` is woken and receives `ObjectDisposedException` (baton release); nobody hangs. |
| **Discard With Pending Waiters** | Discarding frees a permit, so a pending waiter wakes up and receives a freshly created instance. |

