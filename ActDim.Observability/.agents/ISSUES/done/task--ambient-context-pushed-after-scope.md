---
slug: ambient-context-pushed-after-scope
type: task
status: done
priority: high
created: 2026-08-15
updated: 2026-08-15
---

# Task: Ambient Properties Pushed After the Scope Never Reach the Span

## Description
Since ADR-008 the ambient context was snapshotted into span attributes by `BeginScope`, so anything set afterwards never reached the trace:

```csharp
using (logger.BeginScope("ImportBatch"))
using (callContext.SetStatus("Downloading"))   // never exported
```

`SetStatus` / `ReportProgress` / `PushTags` are meant to be called *during* an operation as it progresses, so this was the common ordering, not the exotic one.

## Resolution
See ADR-011. Exporting a property at push time makes the store a telemetry concept, so the store itself was separated: `IObservabilityContext` now owns telemetry ambient state, while `ICallContext` returns to being a neutral ambient variable bag and remains the backing storage.

- Data properties are written to `Activity.Current` as they are set and restored on dispose.
- The `BeginScope` snapshot stays, covering properties set before a span exists: both orderings now work.
- Control switches are never exported; `SuppressAmbientProperties` suppresses both paths but cannot retract an already-sent attribute.
- `CallContextPropertyNames` → `ObservabilityContextPropertyNames`; telemetry extensions left `ActDim.Practix.Common`; telemetry entries left the `CallContextProperty` enum in `Abstractions`.

## Acceptance
- [x] `SetStatus` / `ReportProgress` / `PushTags` reach the span regardless of ordering relative to `BeginScope`.
- [x] Disposing the ambient scope restores the previous span attribute value, or removes it when there was none.
- [x] A test asserts the push-after-scope ordering (`ObservabilityContext_ExportsProperties_SetAfterTheScopeWasOpened`).
- [x] A test asserts control switches never become span attributes (`ObservabilityContext_NeverExportsControlSwitches`).
