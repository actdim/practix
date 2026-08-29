---
slug: span-enrichment-gated-by-log-level
type: bug
status: done
priority: high
created: 2026-08-14
updated: 2026-08-15
---

# Bug: Span Enrichment Depends on Log Provider Filtering

## Description
`EventObservabilityBridge.Log` enriched the current `Activity` only when `_inner.IsEnabled(logLevel)` returned true, so the trace content depended on the configuration of the log sinks. Measured with `SetMinimumLevel(LogLevel.Warning)`: a span whose scope logged only `Information`/`Debug` lost the whole ambient context.

```
=== span (only filtered logs inside)
  tags:   name=ImportBatch, priority=5           <- ambient tenant.id / status missing
  events: <none>
=== span (one LogWarning inside)
  tags:   ..., tenant.id=Tenant_EU_West, status=Importing
```

Also affected: external scopes, the log event itself and `Activity.AddException`: a `LogDebug(ex, ...)` recorded no exception at all.

## Resolution
See ADR-008. The two signals were separated instead of adding a second level threshold:
- `Log` produces a log record only and never touches the span.
- `BeginScope` owns the span and writes ambient context, external scopes and scope state regardless of log level.
- A logged `Exception` still reaches the span via `Activity.AddException`, deliberately ungated (`EventObservabilityOptions.RecordExceptionsOnSpan`).

Verified with the same probe: both spans now carry `tenant.id` and `status`, and the exception logged at `Debug` produces an `exception` event.

## Acceptance
- [x] Trace enrichment happens independently of logger provider filtering.
- [x] A test covers a factory whose providers filter out the level while the span still receives the ambient context (`EventObservabilityBridge_Log_EnrichesSpan_RegardlessOfLogLevelFiltering`).
