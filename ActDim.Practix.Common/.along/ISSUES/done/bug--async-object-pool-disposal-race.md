---
protocol: along
protocol_version: "2.2.26"
slug: bug--async-object-pool-disposal-race
type: bug
status: done
priority: high
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [pooling, async-object-pool, concurrency, race-condition]
---

# Fix AsyncObjectPool Disposal Race Condition in ReturnAsync and DiscardAsync

## Problem Description
In `AsyncObjectPool<T>`:
`ReturnAsync` checked `Volatile.Read(ref _disposed) != 0` prior to `_items.Enqueue(item)` and `_semaphore.Release()`.
If `DisposeAsync()` executed concurrently between this check and `_semaphore.Release()`:
1. `DisposeAsync()` set `_disposed = 1`, drained existing items in `_items`, and disposed `_semaphore`.
2. `ReturnAsync` then resumed, enqueued `item` into `_items`, and invoked `_semaphore.Release()`.
3. `_semaphore.Release()` threw `ObjectDisposedException`, and `item` was trapped inside `_items` without ever having `disposer` invoked, leaking the resource.
4. Similarly, in `DiscardAsync`, a concurrent `DisposeAsync` could cause `_semaphore.Release()` to throw `ObjectDisposedException`.

## Acceptance Criteria
- [x] `ReturnAsync` handles concurrent disposal by double-checking `_disposed`, catching `ObjectDisposedException` on `_semaphore.Release()`, and draining/disposing the returned item.
- [x] `DiscardAsync` guards `_semaphore.Release()` against concurrent semaphore disposal.
- [x] `DisposeAsync` performs a post-semaphore drain to guarantee no straggler items remain in `_items`.
- [x] Concurrency unit test reproducing/validating concurrent ReturnAsync during DisposeAsync passes (`DisposeAsync_WithConcurrentReturns_CleansUpAllItemsWithoutException`).
- [x] All 249 unit tests in Common.Tests pass cleanly.

