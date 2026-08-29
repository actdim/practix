---
slug: activity-source-registry-unbounded
type: task
status: open
priority: low
created: 2026-08-15
updated: 2026-08-15
---

# Task: `ActivitySourceRegistry` Grows Without Bound

## Description
`ActivitySourceRegistry` caches every `ActivitySource` forever by design, so that listener registration stays valid. Nothing states that the set of source names must be bounded, and nothing prevents a caller from pushing a dynamic name:

```csharp
observability.PushActivitySourceName($"Worker.{tenantId}");   // a new ActivitySource per tenant, kept for the process lifetime
```

Each entry also stays visible to every registered `ActivityListener`, so the cost is not only memory.

## Proposal
State the constraint in the doc comment of `ActivitySourceRegistry` and of `PushActivitySourceName`: source names identify a component, not an instance of work. Optionally cap the registry size and fall back to the default source with a one-time warning when the cap is exceeded, so misuse surfaces instead of leaking quietly.

## Acceptance
- [ ] The bounded-set requirement is documented where a caller will see it.
- [ ] Misuse is at least detectable, whether by a cap or by a diagnostic.
