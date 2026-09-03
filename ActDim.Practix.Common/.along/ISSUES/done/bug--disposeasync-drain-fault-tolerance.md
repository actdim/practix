---
protocol: along
slug: bug--disposeasync-drain-fault-tolerance
type: bug
status: done
priority: high
created: 2026-08-31
updated: 2026-08-31
completed: 2026-08-31
agent: antigravity
tags: [pooling, async, disposal, resilience]
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Bug: Fault-Tolerant Idle Object Draining in AsyncObjectPool.DisposeAsync

## Problem
In `AsyncObjectPool<T>.DisposeAsync()`, when draining idle objects from `_items`, `await DisposeItemAsync(item)` was invoked without a `try/catch` block. If the disposer of one object threw an exception (e.g. native C++ wrapper failure or runtime fault), the draining loop aborted abruptly. Consequently, remaining idle objects were never drained or disposed (causing memory/resource leaks), and `_semaphore.Dispose()` was skipped.

## Solution
1. Wrapped `await DisposeItemAsync(item)` inside the draining loop in `try/catch (Exception ex)`.
2. Collected thrown exceptions into a lazily-allocated `List<Exception>`.
3. Ensured all remaining items in `_items` are drained and disposed.
4. Ensured `_semaphore.Dispose()` is called.
5. Threw `AggregateException` containing all collected failures at the end of `DisposeAsync()`.
6. Added unit test `DisposeAsync_DrainsAllIdleObjects_EvenWhenDisposerThrows_AndThrowsAggregateException` in `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`.

