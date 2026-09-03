---
protocol: along
protocol_version: "2.2.18"
slug: architecture
title: System Architecture & Flow
type: architecture
created: 2026-08-31
updated: 2026-09-03
tags: [architecture, boundaries, subsystems, design-patterns]
---

# System Architecture & Flow

`ActDim.Practix.Common` provides the shared foundational infrastructure, concurrency primitives, caching proxies, compression engines, memory management, and ambient context for the ActDim.Practix framework.

---

## Subsystem Vector Decomposition

The library is organized into 7 distinct, decoupled functional subsystems:

```
                      +----------------------------------+
                      |      ActDim.Practix.Common       |
                      +----------------------------------+
                                        |
       +-----------------+--------------+--------------+-----------------+
       |                 |              |              |                 |
+--------------+ +---------------+ +----------+ +-------------+ +---------------+
| Context/     | | Pooling/      | | Caching/ | | Compression | | Collections/  |
| AsyncLocal   | | Bounded FIFO  | | Memory & | | GZip/Brotli | | Concurrent,   |
| Ambient State| | Object Pool   | | Distrib  | | Tar/Zip BCL | | WeakTable,    |
| Zero-DI Logs | | Discard/Drain | | Proxies  | | Codecs      | | CompositeKey  |
+--------------+ +---------------+ +----------+ +-------------+ +---------------+
                                        |
                         +--------------+--------------+
                         |                             |
                 +---------------+             +---------------+
                 | Memory & Disp |             | Extensions &  |
                 | ArrayPool,    |             | Utilities     |
                 | Recyclable    |             | Zero-Alloc IO,|
                 | StreamManager |             | Guards, Id    |
                 +---------------+             +---------------+
```

---

## Subsystem Roles & Boundaries

1. **Ambient Execution Context ([`topic--ambient-context.md`](./topic--ambient-context.md))**:
   - Manages asynchronous execution context via `AsyncLocal<ImmutableDictionary<string, object>>`.
   - Propagates scoped `IServiceProvider`, `ClaimsPrincipal`, `CancellationToken`, and ambient loggers down async call branches.

2. **Asynchronous Object Pooling ([`topic--async-object-pool.md`](./topic--async-object-pool.md))**:
   - Manages bounded pools of reusable resources (`AsyncObjectPool<T>`) coordinated by `SemaphoreSlim` and `ConcurrentQueue<T>`.
   - Supports leased handle lifecycle (`PooledObject`), `DiscardAsync` for corrupted instances, and `DisposeAsync` fault-tolerant draining.

3. **Caching Proxies ([`topic--caching-proxies.md`](./topic--caching-proxies.md))**:
   - Transparently wraps synchronous and asynchronous (`Task<T>`, `ValueTask<T>`) delegates with caching behavior (`MemoryCachingProxy`, `DistributedCachingProxy`).
   - Handles binary payload serialization via `IBinarySerializer`.

4. **Compression & Archiving ([`topic--compression-and-archives.md`](./topic--compression-and-archives.md))**:
   - High-performance compression facade (`CompressionManager`) built exclusively on the .NET 10 BCL.
   - Stream and buffer compression for GZip, Deflate, Brotli, and streaming entry traversal for ZIP and TAR archives.

5. **Specialized Collections ([`topic--concurrent-collections.md`](./topic--concurrent-collections.md))**:
   - `ConcurrentFactoryDictionary<TKey, TValue>`: Lock-free exactly-once factory execution with exception-retry guarantees.
   - `WeakTable<K, V>`: Weak reference table supporting custom `IEqualityComparer<K>`.
   - `CompositeKey`: Structural value-equality composite key struct.

6. **Memory & Disposal Lifecycle ([`topic--memory-and-disposal.md`](./topic--memory-and-disposal.md))**:
   - `ArrayPoolBufferOwner<T>` / `IBufferOwner<T>`: Deterministic rented buffer ownership.
   - `MemoryManager.Default`: Process-wide `RecyclableMemoryStreamManager` configured to prevent heap allocations.
   - `DisposableAction`: Atomic idempotent disposal wrapper; `ReachabilityObserver`: GC finalization observer.

7. **Extensions & Common Utilities ([`topic--extensions-and-utilities.md`](./topic--extensions-and-utilities.md))**:
   - `StreamExtensions`: Zero-allocation stream conversions, UTF-8 without BOM decoding, and chunked writers.
   - `RandomId`: Cryptographic, URL-safe random identifier generator (Base62, Base58, Crockford Base32).
