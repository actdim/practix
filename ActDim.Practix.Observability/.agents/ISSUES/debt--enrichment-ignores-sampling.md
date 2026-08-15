---
slug: enrichment-ignores-sampling
type: debt
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
---

# Debt: Enrichment Runs for Spans Nobody Will Export

## Description
Three avoidable costs on the hot path:

1. **Sampling is ignored.** `Activity.IsAllDataRequested` is not checked anywhere. With 1% sampling the bridge still reflects over DTOs, flattens graphs and writes tags for the 99% of spans that will be discarded. The same applies to `ObservabilityContext.PushExported`, which writes a tag on every property push.

2. **Exported properties are re-written on every scope.** `EnrichSpanFromScope` iterates the whole `ExportedKeys` set and re-issues `SetTag` for each entry on every `BeginScope`, even though `PushExported` already wrote each of them at push time. Three nested scopes do three redundant passes writing identical values. Only properties pushed *before* the span existed actually need the pass.

3. **Every exported push costs two ambient pushes and an immutable-set allocation.** `PushExported` pushes the value and then re-pushes a new `ImmutableHashSet` containing the key. For a loop reporting progress this doubles the ambient churn.

None of these is a correctness problem; all are waste in exactly the code that runs most often.

## Proposal
- Skip enrichment and immediate export when the current activity is not recording (`is { IsAllDataRequested: true }`).
- Track which exported keys have already been applied to a given activity, or record at push time whether a span was available and only replay the ones that were not.
- Consider a single ambient entry holding both the value and its exported flag, instead of a parallel key set.

## Acceptance
- [ ] No reflection or tag writes happen for a non-recording activity.
- [ ] Nested scopes do not re-write properties already on the span.
- [ ] A test asserts that a non-sampled span is left untouched.
