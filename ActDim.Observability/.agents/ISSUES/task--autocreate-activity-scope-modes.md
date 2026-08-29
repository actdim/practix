---
slug: autocreate-activity-scope-modes
type: task
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
---

# Task: Scope-to-Span Behaviour Depends on Where the Code Is Called From

# Description
`BeginScope` starts a span only when `Activity.Current` is null. The same code therefore produces a different trace shape depending on its caller: in a background worker the scope becomes a span, inside an HTTP request it only adds attributes to the request span. Nested operations of a request are invisible as spans, while the identical helper called from a worker is a span of its own.

`AutoCreateActivityOnScope` being a `bool` hides this: it reads as "create spans, yes or no", when the real behaviour is "create only if there is nothing to attach to".

## Proposal
Replace the flag with an explicit mode:
- `Never`: scopes only enrich the current span;
- `WhenNoActivity`: current behaviour, the default;
- `Always`: every scope is a child span, so a scope means the same thing everywhere.

`Always` is what most users expect from "a scope is an operation", but it multiplies span count, so it must be an opt-in with that trade-off stated in the documentation.

## Acceptance
- [ ] The three modes exist and are covered by tests.
- [ ] The default keeps today's behaviour.
- [ ] The trade-off of `Always` is documented where the option is declared.
