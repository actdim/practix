---
slug: telemetry-tag-namespacing
type: debt
status: done
priority: critical
created: 2026-08-14
updated: 2026-08-14
---

# Debt: Silent Telemetry Tag Overwrites in `EventObservabilityBridge`

## Problem
All telemetry sources were merged into one flat dictionary attached to every `ActivityEvent`, with message-template placeholders written last. Any placeholder whose `ToOtelName` form matched a bridge-owned or ambient key overwrote it without a trace.

Reproduced in the real DI path with `logger.LogInformation(new EventId(42, "OrderProcessed"), "Order {OrderId} moved to {Status}, note: {Message}, code {EventId}", ...)`:

```
event.id = 999                  <- was 42
message  = "hand-written note"  <- was the formatted log message
status   = "Shipped"            <- was ambient SetStatus("Downloading")
```

Everyday calls such as `LogError(ex, "Failed: {Message}", ex.Message)` and `LogInformation("Order {OrderId} → {Status}", id, status)` hit this.

Related defects fixed together:
- Log level was never written to telemetry at all: an `ActivityEvent` from `LogError` was indistinguishable from `LogDebug`.
- Ambient context and external scopes were copied into every event of a span instead of being written once.
- Exceptions were recorded as ad-hoc `exception.*` tags instead of the OpenTelemetry convention.
- `ToOtelName` did not handle `{@value}` / `{$value}`, emitting attributes named `@value`.

## Resolution
See ADR-005 and ADR-006.
- Ambient `ICallContext` data and external scopes → span attributes; log call → `ActivityEvent`.
- Bridge-owned tags moved to the reserved `log.*` namespace: `log.message`, `log.level`, `log.event.id`, `log.collisions` (`ObservabilityTagNames`).
- All writes go through `TelemetryTagCollector` with `TagCollisionBehavior` (`KeepFirst` default, `Overwrite`, `Throw`); collisions are counted and exported as `log.collisions`.
- `Activity.AddException(exception)` replaces manual exception tags.
- `ToOtelName` strips leading `@` / `$`.

## Acceptance
- [x] A placeholder named `{Message}` / `{Status}` / `{EventId}` no longer destroys bridge or ambient data.
- [x] Collisions that remain are counted and exported.
- [x] `TagCollisionBehavior.Throw` fails the call, for use in tests.
- [x] Exceptions produce a standard `exception` event.
- [x] 13 tests in `Tests/Observability.Tests` pass.

## Follow-ups
- [bug--span-enrichment-gated-by-log-level](../bug--span-enrichment-gated-by-log-level.md)
- [task--logs-without-active-span](../task--logs-without-active-span.md)
- [feat--trace-context-in-text-logs](../feat--trace-context-in-text-logs.md)
- [feat--message-template-analyzer](../feat--message-template-analyzer.md)
