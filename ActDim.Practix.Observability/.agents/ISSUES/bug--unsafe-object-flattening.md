---
slug: unsafe-object-flattening
type: bug
status: open
priority: critical
created: 2026-08-15
updated: 2026-08-15
---

# Bug: Object Flattening Can Crash or Flood the Process

## Description
`EventObservabilityHelper.FlattenPairs` walks public properties by reflection with no guard rails, so an arbitrary object handed to `BeginScope` can take the process down. Observability must never be able to break the application it observes.

1. **Cycles cause `StackOverflowException`.** There is no visited set and no depth limit ([EventObservabilityHelper.cs:77](../../ActDim.Practix.Observability/EventObservabilityHelper.cs#L77)). Any parent/child navigation — EF entities, trees, doubly linked structures — recurses forever. A `StackOverflowException` cannot be caught: the process dies.
2. **A throwing property getter propagates into business code.** `prop.GetValue(obj)` ([EventObservabilityHelper.cs:84](../../ActDim.Practix.Observability/EventObservabilityHelper.cs#L84)) is not guarded. A lazy navigation property on a disposed `DbContext`, or a getter doing I/O, throws out of `BeginScope`.
3. **No limit on the number of attributes.** `byte[]`, a thousand-element list or a configuration dictionary are all `IEnumerable` and are expanded element by element. One unlucky DTO sends megabytes to the trace backend.
4. **Collections at the root produce nameless keys.** With an empty prefix the key becomes `[0]`, `[1]` — not a usable attribute name.
5. **`null` property values silently vanish.** `activity.SetTag(key, null)` removes the attribute, so "field is empty" and "field does not exist" become indistinguishable.

## Proposal
- Track visited references (reference-equality set) and cap recursion depth; emit a marker attribute when the graph is truncated rather than truncating silently.
- Cap the total number of produced pairs, with the same visible marker.
- Wrap `GetValue` and swallow, recording the failure as a value like `<error>` instead of losing the whole scope.
- Give root collections a real prefix.
- Decide explicitly what a `null` value means — either skip it or write a sentinel — and document it.

Limits belong in `EventObservabilityOptions` with sane defaults.

## Acceptance
- [ ] A cyclic graph produces bounded output and no crash.
- [ ] A throwing getter does not escape `BeginScope`.
- [ ] Depth, breadth and total attribute count are bounded and configurable, and truncation is visible in the telemetry.
- [ ] Tests cover cycle, throwing getter, large collection, root collection and `null` value.
