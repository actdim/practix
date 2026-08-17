---
date: 2026-08-14
slug: telemetry-tag-namespacing
agent: claude-opus-5
branch: main
commit: pending
summary: Fixed silent telemetry tag overwrites in EventObservabilityBridge — span/event split, reserved log.* namespace, collision policy and counter, Activity.AddException, destructuring hints in ToOtelName.
---

# Session Log: Telemetry Tag Ownership in `EventObservabilityBridge`

## What changed & why

The session started as a design discussion about a Roslyn analyzer for `ILogger` message templates and turned into a bug hunt, because measuring what the SDK already covers changed the premise.

**Measured, not assumed:**
- `CA2017` (not `CA2254`) already reports placeholder/argument count mismatches, including in `BeginScope`, and is enabled by default on net10.0. `CA2253` and `CA2254` exist but stay silent under default settings. A custom analyzer duplicating count checks would add nothing.
- Probing the bridge with a real `Activity` and, separately, through the DI path with OpenTelemetry 1.17 revealed that message-template placeholders silently overwrote bridge-owned and ambient tags: `{Message}` destroyed the formatted message, `{Status}` destroyed the ambient status, `{EventId}` destroyed the event id.
- The OTel logs pipeline needed no changes: `LogRecord` already carries `TraceId` / `SpanId` natively and contains only what the caller passed, with ambient data and scopes correctly excluded while `IncludeScopes` is false.

**Implemented (ADR-005, ADR-006):**
- Ambient `ICallContext` data and external scopes now become **span** attributes; the log call becomes an `ActivityEvent`. This removes the cross-source collisions by construction and stops copying ambient data into every event of a span.
- Bridge-owned tags moved into the reserved `log.*` namespace: `log.message`, `log.level`, `log.event.id`, `log.collisions`. Dotted form was chosen over `log.event_id` because `ToOtelName` emits dotted names exclusively.
- `log.level` is new — the log level was previously absent from telemetry entirely.
- `TelemetryTagCollector` is now the single write path, applying `TagCollisionBehavior` (`KeepFirst` default, `Overwrite`, `Throw`) and counting every collision; a non-zero count is exported as `log.collisions`.
- Exceptions go through `Activity.AddException`, producing the standard `exception` event instead of ad-hoc tags.
- `ToOtelName` strips leading `@` / `$` so `{@user}` no longer becomes an attribute named `@user`.

## Files touched
- `ActDim.Observability/EventObservabilityBridge.cs`
- `ActDim.Observability/EventObservabilityOptions.cs`
- `ActDim.Observability/EventObservabilityHelper.cs`
- `ActDim.Observability/TagCollisionBehavior.cs` (new)
- `ActDim.Observability/TelemetryTagCollector.cs` (new)
- `ActDim.Observability/ObservabilityTagNames.cs` (new)
- `Tests/Observability.Tests/ObservabilityTests.cs`
- `.agents/DECISIONS.md`, `.agents/ISSUES.md`, `.agents/ISSUES/*`, `.agents/CONTEXT.md`, `.agents/GLOSSARY.md`, `.agents/HISTORY.md`

## Decisions
- ADR-005 — telemetry tag ownership: span/event split and reserved `log.*` namespace.
- ADR-006 — exceptions recorded via `Activity.AddException`.

## Issues advanced
- Closed: `debt--telemetry-tag-namespacing`.
- Opened: `bug--span-enrichment-gated-by-log-level`, `task--logs-without-active-span`, `feat--trace-context-in-text-logs`, `feat--message-template-analyzer`.

## Verification
Full solution builds. `Tests/Observability.Tests`: 13 passed, 0 failed (8 pre-existing tests updated for the new layout, 5 added: reserved tags survive placeholders, collision counting with `KeepFirst`, `Throw` behaviour, exception event, destructuring hints).

## Gaps / follow-ups
- The breaking-change question (`LegacyFlatTags` compatibility flag) was raised three times and never answered; the clean layout was implemented without a compatibility branch. Consumers querying `message` / `event.id` / ambient tags on events must be updated.
- Pre-existing `CS8604` warning at `EventObservabilityBridge.BeginScope` was left untouched — out of the requested scope.
- Attribute names differ between pipelines for the same value (`OrderId` in `LogRecord` vs `order.id` in the span). Not a bug; needs a deliberate decision.
