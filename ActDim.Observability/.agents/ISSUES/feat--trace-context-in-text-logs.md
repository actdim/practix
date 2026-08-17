---
slug: trace-context-in-text-logs
type: feat
status: open
priority: medium
created: 2026-08-14
updated: 2026-08-14
---

# Feature: Trace Context in Text Log Sinks

## Description
`AddEventObservability` never configures `LoggerFactoryOptions.ActivityTrackingOptions`, whose default is `None`. Text sinks (Console, File, Serilog) therefore emit no `TraceId` / `SpanId`, and a log line cannot be linked back to its span.

Verified: the OpenTelemetry logs pipeline is unaffected — `OpenTelemetryLoggerProvider` fills `LogRecord.TraceId` / `SpanId` from `Activity.Current` natively, and ambient context and external scopes correctly stay out of the `LogRecord` while `IncludeScopes` remains false. Only text sinks lack the correlation.

## Proposal
```csharp
services.Configure<LoggerFactoryOptions>(o =>
    o.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId);
```
Expose it as an option, since combining it with `IncludeScopes = true` on the OTel logger duplicates trace context into log attributes.

## Acceptance
- [ ] Text sinks receive trace context by default.
- [ ] The behaviour is switchable through `EventObservabilityOptions`.
- [ ] A test asserts the scope reaches a recording provider.
