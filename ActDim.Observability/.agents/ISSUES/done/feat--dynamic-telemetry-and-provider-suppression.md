---
slug: dynamic-telemetry-and-provider-suppression
type: feat
status: done
priority: high
created: 2026-08-14
updated: 2026-08-14
---

# Feature: Dynamic Telemetry & Selective Provider Suppression

## Summary
- Implemented `callContext.SuppressExternalScopes()`, `callContext.SuppressCallContext()`, `callContext.SuppressConsole()`, and `callContext.SuppressProviders("File")`.
- Integrated official .NET `[ProviderAlias]` resolution in `EventObservabilityBridge`.
- Verified clean build and unit tests passing.
