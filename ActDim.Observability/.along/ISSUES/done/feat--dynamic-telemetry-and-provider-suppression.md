---
protocol: along
slug: dynamic-telemetry-and-provider-suppression
type: feat
status: done
priority: high
created: 2026-08-14
updated: 2026-08-14
completed: 2026-08-14
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Feature: Dynamic Telemetry & Selective Provider Suppression

## Summary
- Implemented `callContext.SuppressExternalScopes()`, `callContext.SuppressCallContext()`, `callContext.SuppressConsole()`, and `callContext.SuppressProviders("File")`.
- Integrated official .NET `[ProviderAlias]` resolution in `EventObservabilityBridge`.
- Verified clean build and unit tests passing.
