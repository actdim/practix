---
protocol: along
date: 2026-09-09
slug: async-object-pool-disposal-race
agent: antigravity
branch: main
commit: pending
summary: Fixed race condition in AsyncObjectPool during concurrent ReturnAsync/DiscardAsync and DisposeAsync
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [bug--async-object-pool-disposal-race]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Fix AsyncObjectPool Disposal Race Condition

## Problem
In `AsyncObjectPool<T>`:
`ReturnAsync` checked `_disposed` before `_items.Enqueue` and `_semaphore.Release()`. If `DisposeAsync()` ran concurrently, `_semaphore.Release()` threw `ObjectDisposedException` and the returned item remained trapped in `_items` without being disposed, leaking resources. `DiscardAsync` had an analogous race.

## Solution
1. In `ReturnAsync`, added double-check on `_disposed`, wrapped `_semaphore.Release()` to catch `ObjectDisposedException`, and drained the item with disposer.
2. In `DiscardAsync`, guarded `_semaphore.Release()` against concurrent disposal.
3. In `DisposeAsync`, added post-semaphore drain to guarantee no straggler items remain in `_items`.
4. Added concurrency unit test `DisposeAsync_WithConcurrentReturns_CleansUpAllItemsWithoutException` in `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`.
