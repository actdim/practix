---
protocol: along
protocol_version: "4.4.6"
date: 2026-10-06
slug: async-object-pool-dispose-hang
agent: claude-code
branch: main
commit: d45499c
summary: 'AsyncObjectPool: pending GetAsync waiters no longer hang on DisposeAsync (baton release, semaphore never disposed)'
milestone: v2.0.0-along-transition
issues_advanced: [bug--async-object-pool-discard-and-disposed-semaphore-leak]
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Async object pool dispose hang

## Summary
AsyncObjectPool: pending GetAsync waiters no longer hang on DisposeAsync (baton release, semaphore never disposed)

Trigger: code review finding "Callers waiting on it when it is disposed hang forever." Root cause: `DisposeAsync`
called `_semaphore.Dispose()`, and `SemaphoreSlim.Dispose()` does not complete pending `WaitAsync` tasks.

## Work Completed
- `Pooling/AsyncObjectPool.cs`:
  - Semaphore created as `SemaphoreSlim(maxSize)` (no max count) and never disposed; all `catch (ObjectDisposedException)` around `Release` and the second post-dispose drain loop removed.
  - "Baton" wake-up: `DisposeAsync` releases one permit before draining idle items; every awakened `GetAsync` waiter sees `_disposed`, releases one permit for the next waiter and throws `ObjectDisposedException`. No per-call `CancellationTokenSource`, no waiter counter.
  - Invariant: every acquired permit is released exactly once on every path (return, discard, factory failure, disposal during factory, post-disposal wake-up).
  - Class-level `<remarks>` documents the mechanism.
- `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`: added `DisposeAsync_WakesAllPendingWaiters_WithObjectDisposedException`, `ReturnAfterDisposeAsync_InvokesDisposer`, `DiscardAsync_WakesPendingWaiter_WithFreshInstance`.
- `docs/topic--async-object-pool.md`: "Waking Pending Waiters on Disposal" section, concurrency model and invariants table updated.
- Issue `bug--async-object-pool-discard-and-disposed-semaphore-leak`: defect 3 (hang) recorded; defects 2 and 3 resolved; defect 1 (public `DiscardAsync(T)` accepts foreign instances) still open, issue stays in-progress.

## Decisions
- Rejected returning to the previous `Channel<T>` implementation (MLWeb `MapServer/AsyncObjectPool.cs`): discard / factory failure there free capacity only in a counter and never wake `ReadAsync` waiters; "free slot" sentinels in the item FIFO would break lazy reuse-first semantics. Recorded in the issue, no ADR.

## Code Review & Blast Radius
- Tests: `dotnet test Tests/Common.Tests -v q` - 256/256 passed after the pool change; `dotnet test Tests/Common.Tests --no-build -v q --filter FullyQualifiedName~AsyncObjectPool` - 14/14 passed in 5 consecutive runs. New hang test fails with `TimeoutException` on the old semaphore-dispose code.
- Blast radius: public API unchanged (`GetAsync`, `DiscardAsync`, `DisposeAsync`, `PooledObject`). Behavior change only for callers pending during disposal (now `ObjectDisposedException` instead of hanging). `code-review-graph` MCP was unavailable (connection closed); static search: `AsyncObjectPool` is consumed only by its tests within this repo.
- Housekeeping: a temporary root umbrella issue (`bug--pool-dispose-hangs-waiters`) was created to unblock the test-file edit gate and then removed; unrelated milestone rewrites of v1.3.0/v1.4.0 by `along milestone sync` were reverted. `along test` synthesized `.along/scripts/test.py` for this subproject.
