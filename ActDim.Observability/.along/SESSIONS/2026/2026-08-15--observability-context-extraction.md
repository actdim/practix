---
protocol: along
date: 2026-08-15
slug: observability-context-extraction
agent: claude-opus-5
branch: main
commit: pending
summary: Extracted IObservabilityContext from ICallContext: telemetry ambient state now exports to the span at push time, while ICallContext returns to being a neutral variable store.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: Extracting the Observability Context

## What changed & why

Follow-up to `2026-08-15--log-record-span-separation`, which left one sharp edge: after ADR-008 the ambient context was snapshotted by `BeginScope`, so `SetStatus` called *inside* the scope: the normal way to report progress as an operation runs: never reached the span.

The fix could not just be "write to `Activity.Current` on push": that turns the store into a telemetry concept, and the store was `ICallContext`, a neutral ambient bag in `Abstractions`. The boundary turned out to be broken already: `CallContextPropertyNames` consisted entirely of telemetry keys, so the neutral abstraction knew about console providers and `ActivitySource`.

So the concept was separated (ADR-011):
- `IObservabilityContext` + `ObservabilityContext` in `ActDim.Observability`, registered by `AddEventObservability`. `ICallContext` stays the backing store, so ambient values remain readable by non-telemetry consumers (the planned console spinner).
- Data properties export to `Activity.Current` at push time; disposing restores the previous attribute value or removes an absent one. The span is captured at push, since `Activity.Current` may differ on dispose.
- The `BeginScope` snapshot stays: it covers properties set before a span exists. Both orderings now work.
- Control switches are never exported. A test lock this in: a scope with `SuppressConsole` + `SuppressProviders` + `PushActivitySourceName` leaves the span with zero attributes.

A failing test exposed one more semantic question: `SuppressAmbientProperties` (renamed from `SuppressCallContext`) must suppress the immediate export as well, not only the snapshot: but it cannot retract an attribute already sent, so the suppression has to be opened before the pushes it is meant to hide. Both are now stated in the interface documentation.

## Files touched
- `ActDim.Observability/IObservabilityContext.cs` (new), `ObservabilityContext.cs` (new), `ObservabilityContextPropertyNames.cs` (new)
- `ActDim.Observability/EventObservabilityExtensions.cs`: DI registration
- `ActDim.Observability/EventObservabilityBridge.cs`, `EventObservabilityLoggerFactory.cs`: new property names
- `ActDim.Practix.Abstractions/Context/CallContextProperty.cs`: telemetry entries removed, `CallContextPropertyNames` gone
- `ActDim.Practix.Common/Context/CallContextExtensions.cs`: deleted, telemetry methods moved
- `Tests/Observability.Tests/ObservabilityTests.cs`
- `.agents/DECISIONS.md`, `.agents/ISSUES.md`, `.agents/ISSUES/*`, `.agents/CONTEXT.md`, `.agents/GLOSSARY.md`, `.agents/HISTORY.md`

## Decisions
- ADR-011: `IObservabilityContext` separated from `ICallContext`.

## Issues advanced
- Closed: `task--ambient-context-pushed-after-scope`.
- Updated: `feat--interactive-console-context-spinner` now points at `ObservabilityContextPropertyNames`.

## Verification
Full solution builds, `Tests/Observability.Tests`: 23 passed, 0 failed (two new: push-after-scope export with restore-on-dispose, and control switches never exported).

## Gaps / follow-ups
- The console spinner will read ambient values through `IObservabilityContext.Data`; the issue text was updated but the design was not revisited.
- `ActivitySourceRegistry` still never evicts: source names must be a bounded set, not yet stated in its doc comment.
- A log call made without any scope still produces no trace data; that remains the documented model.
