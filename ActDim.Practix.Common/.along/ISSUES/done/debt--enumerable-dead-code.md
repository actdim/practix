---
protocol: along
slug: debt--enumerable-dead-code
type: debt
status: done
priority: low
created: 2026-08-15
updated: 2026-08-15
completed: 2026-08-15
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# debt: Remove commented-out code from EnumerableExtensions.cs

120+ lines of dead, commented-out code remain in `EnumerableExtensions.cs`:
- `PartitionHelper<T>` class (~40 lines)
- `Zip<TFirst, TSecond, TThird, TResult>` overloads
- `Traverse<T>` methods
- `AsDuckEnumerable<T>`
- `CopyTo<T>`

Remove all per `AGENTS.md` conventions. Verify no usage before removal (already confirmed: all commented out).
