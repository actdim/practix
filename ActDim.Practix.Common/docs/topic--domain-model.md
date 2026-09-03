---
protocol: along
protocol_version: "2.2.18"
slug: domain-model
title: Domain Model & Vocabulary
type: domain-model
created: 2026-08-31
updated: 2026-09-03
tags: [domain-model, entities, contracts, error-handling, vocabulary]
---

# Domain Model & Vocabulary

Core domain concepts, resource ownership models, state transitions, and error handling semantics across `ActDim.Practix.Common`.

---

## Domain Glossary

| Term | Definition | Primary Type |
| :--- | :--- | :--- |
| **Ambient Context** | Immutable dictionary of execution metadata bound to the current asynchronous execution flow (`AsyncLocal`). | `AmbientContext` |
| **Pooled Object Handle** | Leased wrapper around a pooled resource that returns the resource to the pool on disposal or discards it on failure. | `AsyncObjectPool<T>.PooledObject` |
| **Discard Operation** | Unbinds a corrupted or faulted instance from the pool, invokes disposer, and restores semaphore capacity. | `AsyncObjectPool<T>.DiscardAsync` |
| **Caching Proxy** | Higher-order delegate decorator intercepting invocations to read from or populate cache storage. | `IMemoryCachingProxy`, `IDistributedCachingProxy` |
| **Buffer Owner** | Owned buffer lease carrying valid length that returns rented memory to `ArrayPool` on disposal. | `IBufferOwner<T>`, `ArrayPoolBufferOwner<T>` |
| **Atomic Disposal Action** | Thread-safe single-execution callback executed at most once upon `Dispose()` or `DisposeAsync()`. | `DisposableAction`, `DisposableAsyncAction` |
| **Random ID Generator** | CSPRNG-backed uniform random token generator supporting Base62, Base58, and Crockford Base32. | `RandomId` |
| **Weak Table** | Map holding weak references to keys, automatically cleaned up by GC finalization with custom equality support. | `WeakTable<K, V>` |
| **Composite Key** | Immutable struct representing an ordered tuple of objects evaluated for element-wise value equality. | `CompositeKey` |

---

## State Transitions & Lifecycles

### 1. Object Pool State Machine
```
[Uninstantiated] ---> GetAsync() ---> (Pool Capacity Check via Semaphore)
                           |
            +--------------+--------------+
            |                             |
    [Idle Item in Queue]         [Invoke Factory()]
            |                             |
            +--------------> [Leased Handle (PooledObject)]
                                    |
                    +---------------+---------------+
                    |                               |
              DisposeAsync()                  DiscardAsync()
                    |                               |
       [Return to Queue FIFO]           [Invoke Disposer Delegate]
       [Release Semaphore]              [Release Semaphore Capacity]
```

### 2. Memory Buffer Rental Lifecycle
```
[ArrayPool<T>.Shared]
        |
        +---> ArrayPoolBufferOwner<T>.Rent(size)
                    |
              [IBufferOwner<T> Handle]
              |-- .Array (rented bucket array)
              |-- .Length (exact requested length)
              |-- .Memory (slice: [0..Length])
                    |
              [Dispose()]
                    |
              [Interlocked Exchange] ---> Array returned to ArrayPool
```

---

## Error Handling & Exception Guarantees

1. **Pool Drain Fault-Tolerance**: If exceptions occur while disposing pooled items during `AsyncObjectPool.DisposeAsync()`, the pool completes draining all items and throws an `AggregateException` aggregating all individual errors.
2. **Factory Failure Isolation**: If a factory throws an exception during `AsyncObjectPool.GetAsync()` or `ConcurrentFactoryDictionary.GetOrCreateValue()`, the failed state is pruned immediately, allowing subsequent requests to retry without stale error persistence.
3. **Format Detection Exceptions**: Format sniffing throws `DataFormatException` on invalid magic bytes, and `NotSupportedException` for formats with no BCL codec (e.g. RAR, 7z).
4. **Disposed Buffer Access**: Attempting to access `.Array` or `.Memory` on a disposed `IBufferOwner<T>` throws `ObjectDisposedException`.
