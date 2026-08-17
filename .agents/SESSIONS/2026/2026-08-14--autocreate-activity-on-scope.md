---
date: 2026-08-14
slug: autocreate-activity-on-scope
agent: Antigravity / Gemini 3.7 Flash
branch: main
commit: 773ec0b
summary: Auto-create OpenTelemetry Activity spans on logger.BeginScope when Activity.Current is null, with CallContext ActivitySource override and options default.
---

# Auto-Creation of Activity Spans on BeginScope

## Context & Problem
When logs were written or scopes created outside an ambient span (`Activity.Current == null`), neither logs nor ambient context reached OpenTelemetry tracing pipelines. Developers needed a clean way to ensure that scoped operations automatically start an `Activity` span without requiring manual `ActivitySource` boilerplate, with support for dynamic `ActivitySource` overrides via `ICallContext`.

## What Changed
1. **Abstractions:** Added `CallContextPropertyNames.ActivitySourceName` and enum value `CallContextProperty.ActivitySourceName` in `ActDim.Practix.Abstractions`.
2. **Common:** Added `callContext.PushActivitySourceName(activitySourceName)` extension method in `ActDim.Practix.Common`.
3. **Observability Options:** Added `AutoCreateActivityOnScope` (default `true`) and `DefaultActivitySourceName` (default `Assembly.GetEntryAssembly()?.GetName().Name ?? "ActDim.Practix"`) in `EventObservabilityOptions`.
4. **Registry:** Added internal `ActivitySourceRegistry` backed by `ConcurrentDictionary<string, ActivitySource>` for thread-safe caching and reuse of `ActivitySource` instances.
5. **Bridge Scope Handling:** Updated `EventObservabilityBridge.BeginScope` to start an `Activity` when `Activity.Current == null`, resolving the source name from `CallContext` or options, and wrapping the lifecycle in `ScopeDisposable`.
6. **Tests:** Added `TestAllActivityListener` and 4 unit tests in `ObservabilityTests.cs`.

## Files Touched
- `ActDim.Practix.Abstractions/Context/CallContextProperty.cs`
- `ActDim.Practix.Common/Context/CallContextExtensions.cs`
- `ActDim.Observability/EventObservabilityOptions.cs`
- `ActDim.Observability/ActivitySourceRegistry.cs`
- `ActDim.Observability/EventObservabilityBridge.cs`
- `ActDim.Observability/README.md`
- `Tests/Observability.Tests/ObservabilityTests.cs`

## Decisions
- Recorded ADR-007: Automatic Activity Span Creation on BeginScope with CallContext Source Override in `.agents/DECISIONS.md`.

## Issues Advanced / Closed
- Closed `task--logs-without-active-span` (moved to `.agents/ISSUES/done/task--logs-without-active-span.md`).
