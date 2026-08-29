---
slug: span-tag-restore-assumes-lifo
type: bug
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
---

# Bug: Span Attribute Restore Assumes Strictly Nested Disposal

## Description
`ObservabilityContext.SpanTagScope` captures the attribute value that was on the span before the push and writes it back on dispose. That is correct only while handles are disposed in reverse order of creation. In asynchronous code nothing enforces that: two overlapping property scopes disposed in creation order make the older restore overwrite the newer value, leaving the span with a stale attribute for the rest of the operation.

The same assumption now applies twice, because a single `Push` produces three stacked handles:
- the ambient property itself (`IAmbientContext.PushProperty`),
- the `ExportedKeys` set, re-pushed as a new `ImmutableHashSet` on every export,
- the span attribute restore.

Out-of-order disposal therefore desynchronizes the exported-key set from the property values as well, so `EnrichSpanFromScope` can iterate a key whose value has already been popped: it is skipped silently by the `TryGetValue` guard, which hides the inconsistency instead of surfacing it.

## Proposal
Either detect the out-of-order case (compare the current attribute value with the one written at push; restore only when they still match) and skip the restore otherwise, or keep a per-activity stack of pushed values so the restore always yields the topmost surviving one. The first option is cheap and removes the destructive case; the second is exact.

## Acceptance
- [ ] Disposing two overlapping property scopes in creation order leaves the span with the value of the still-active scope.
- [ ] A test covers the non-nested disposal order explicitly.
