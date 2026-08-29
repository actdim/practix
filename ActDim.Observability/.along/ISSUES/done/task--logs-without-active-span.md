---
protocol: along
slug: logs-without-active-span
type: task
status: done
priority: medium
created: 2026-08-14
updated: 2026-08-14
completed: 2026-08-14
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Task: Decide What Happens to Log Calls Made Outside an Active Span

## Description
When `Activity.Current` is null, `EventObservabilityBridge` writes no telemetry at all: neither the log call nor the ambient context reaches any trace. Everything logged during startup, background work or shutdown that runs outside a span is therefore invisible to the tracing pipeline.

## Resolution
Implemented automatic `Activity` span creation on `logger.BeginScope(...)` when `Activity.Current == null`.
The `ActivitySource` is resolved dynamically:
1. `callContext.PushActivitySourceName("CustomSource")`.
2. `EventObservabilityOptions.DefaultActivitySourceName` (`Assembly.GetEntryAssembly()?.GetName().Name ?? "ActDim.Practix"`).
Covered by tests in `ObservabilityTests.cs` and documented in ADR-007 and README.md.

## Acceptance
- [x] Behaviour is chosen, documented, and covered by a test asserting it deliberately.
