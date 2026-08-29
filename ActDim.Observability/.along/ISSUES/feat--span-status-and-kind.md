---
protocol: along
slug: span-status-and-kind
type: feat
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

# Feature: Span Status and Span Kind

## Description
Two pieces of standard span metadata are never set.

1. **Status.** A logged exception produces an `exception` event, but `ActivityStatusCode` stays `Unset`, so a failed operation is displayed as successful in Jaeger/Grafana and is not counted by error-rate queries.
2. **Kind.** An auto-created span is always `ActivityKind.Internal`. A background worker consuming a queue is a `Consumer`, an inbound handler is a `Server`: backends use kind for service maps and latency breakdowns.

## Open decision on status
Marking the span failed on *any* logged exception is wrong: handled exceptions are logged all the time (retries, expected conflicts, fallbacks). The candidates are:
- set `Error` only for `LogError` / `LogCritical`, leaving `Warning` and below as an event only;
- never set it from a log call and expose it explicitly on the observability context instead (`observability.MarkFailed(...)`), which keeps the "a log call does not shape the trace" rule of ADR-008 intact.

The second option is more consistent with ADR-008 and should be the default unless the first proves more practical.

## Proposal
- Add an explicit way to mark the current operation failed, and decide whether `LogError`/`LogCritical` also does it (option on `EventObservabilityOptions`).
- Allow the scope to declare its kind, e.g. through `LogEvent` or an overload used by the future `StartOperation` helper.

## Acceptance
- [ ] A failed operation is visible as failed in the trace backend.
- [ ] A handled-and-logged exception does not by itself mark the operation failed unless configured to.
- [ ] Span kind can be set without manual `ActivitySource` boilerplate.
