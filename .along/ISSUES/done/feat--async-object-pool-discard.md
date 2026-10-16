---
protocol: along
protocol_version: "4.4.15"
slug: async-object-pool-discard
type: feat
status: done
priority: medium
created: 2026-09-01
updated: 2026-09-01
completed: 2026-09-01
agent: antigravity
tags: [pkg-practix-common, pooling, async, resource-management]
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Implement Discard and DiscardAsync in AsyncObjectPool

## Problem Statement / Background
`ActDim.Practix.Pooling.AsyncObjectPool<T>` managed leased items via bounded channel storage but provided no mechanism to discard corrupted, faulty, or disconnected instances. When a pooled resource encountered a terminal network failure or invalid state, disposing `PooledObject` returned the corrupted instance back into the pool, poisoning subsequent leases.

## Implementation Details
1. Replaced `Channel<T>` with a `ConcurrentQueue<T>` paired with a `SemaphoreSlim` to decouple acquisition concurrency from item storage.
2. Added `DiscardAsync(T item)` on `AsyncObjectPool<T>`, which decrements the active instance count, releases a semaphore slot, and invokes the configured disposer.
3. Added `DiscardAsync()` on the leased `PooledObject` wrapper to enable callers to evict broken items directly from client code.
4. Added thread-safe disposal guards and unit test coverage in `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`.

## Acceptance Criteria
- [x] `AsyncObjectPool<T>` exposes `DiscardAsync(T item)` to dispose faulty items without recycling them.
- [x] Leased `PooledObject` wrapper exposes `DiscardAsync()` that safely unbinds the instance.
- [x] Discarding an item decrements `CreatedCount` and frees a slot for future instantiations.
- [x] Automated test suite in `AsyncObjectPoolTests` passes with zero failures.
