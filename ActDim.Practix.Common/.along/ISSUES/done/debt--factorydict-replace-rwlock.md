---
protocol: along
slug: debt--factorydict-replace-rwlock
type: debt
status: done
priority: medium
created: 2026-08-15
updated: 2026-08-15
completed: 2026-08-15
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# debt: Replace FactoryDictionary with ConcurrentFactoryDictionary

`FuncExtensions.FactoryDictionary<TKey, TValue>` wraps `ConcurrentDictionary` with a `ReaderWriterLockSlim`, but every call unconditionally **enters a write lock**: providing exclusive access with no reader-concurrency benefit. This is strictly worse than a plain `lock` and redundant given `ConcurrentFactoryDictionary<TKey, TValue>` already exists in the Common library.

## Acceptance Criteria
- Deprecate `FactoryDictionary` or replace its internals with `ConcurrentFactoryDictionary`.
- Update `FuncExtensions.Memoize` overload accordingly.
- Existing `Memoize` API signature must remain compatible.
