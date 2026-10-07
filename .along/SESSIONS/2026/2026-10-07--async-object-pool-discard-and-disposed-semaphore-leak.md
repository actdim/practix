---
protocol: along
protocol_version: "4.4.6"
date: 2026-10-07
slug: async-object-pool-discard-and-disposed-semaphore-leak
agent: antigravity
branch: main
commit: HEAD
summary: 'AsyncObjectPool: restrict DiscardAsync visibility to internal, completing defect resolution'
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [bug--async-object-pool-discard-and-disposed-semaphore-leak]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Async object pool discard and disposed semaphore leak

## Summary
AsyncObjectPool: restrict DiscardAsync visibility to internal, completing defect resolution.

## Work Completed
- `Pooling/AsyncObjectPool.cs`:
  - Restricted `DiscardAsync(T item)` to `internal` so external callers cannot pass arbitrary instances to artificially inflate capacity counters.
  - Consumers discard faulty instances solely through `PooledObject.DiscardAsync()`, guaranteeing safe capacity accounting.
- `Tests/Common.Tests/Pooling/AsyncObjectPoolTests.cs`: verified discard and disposal behavior with comprehensive tests.
- Closed issue `bug--async-object-pool-discard-and-disposed-semaphore-leak` as done.

## Decisions
- No new architectural decisions.
