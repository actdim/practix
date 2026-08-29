---
protocol: along
slug: attribute-naming-across-signals
type: task
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Task: One Value, Two Names Across Logs and Traces

## Description
The same value carries different attribute names in the two signals. Measured on one call:

```
LogRecord attributes: OrderId = 7            (as written by the caller)
span attributes:      order.id = 7           (normalized by ToOtelName)
```

Anyone correlating logs and traces has to know both spellings, and a dashboard query cannot be written once.

This is not a defect: it follows from `LogRecord` attributes being produced by `Microsoft.Extensions.Logging` while span attributes go through `EventObservabilityHelper.ToOtelName`: but it is an unrecorded decision.

## Options
1. Accept and document: logs keep the developer's spelling, traces follow the OpenTelemetry convention.
2. Normalize both, which means shaping the log state before it reaches the providers: reopening the ADR-008 rule that a log call is passed through untouched.
3. Stop normalizing span attributes and keep the caller's spelling everywhere, losing OTel-convention naming.

Option 1 is the likely answer; the point of the issue is to state it deliberately and record an ADR.

## Acceptance
- [ ] The choice is recorded as an ADR and mentioned in the package documentation.
