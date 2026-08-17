---
date: 2026-08-14
slug: selective-provider-suppression
agent: antigravity
branch: main
commit: pending
summary: Implemented selective provider suppression (SuppressConsole, SuppressProviders) based on official .NET ProviderAliasAttribute and custom alias registration.
---

# Session Log: Selective Provider Suppression

## Summary of Changes
1. **Selective Provider Suppression API:**
   - Added `CallContextPropertyNames.SuppressConsole` and `SuppressedProviders`.
   - Added `callContext.SuppressConsole()` and `callContext.SuppressProviders("Console", "File")` extension methods.
   - Added `RegisterProviderAlias` to `EventObservabilityOptions`.

2. **Core Enrichment Logic:**
   - Updated `EventObservabilityBridge` to inspect active suppression flags and filter log execution for matching provider types/aliases (`[ProviderAlias]`).

3. **Verification & Testing:**
   - Updated `ObservabilityTests.cs` to test `SuppressConsole()` and `SuppressProviders()`.
   - Confirmed all 7 unit tests pass clean (`Passed: 7, Failed: 0`).

## Files Touched
- `ActDim.Practix.Abstractions/Context/CallContextProperty.cs`
- `ActDim.Practix.Common/Context/CallContextExtensions.cs`
- `ActDim.Observability/EventObservabilityOptions.cs`
- `ActDim.Observability/EventObservabilityBridge.cs`
- `ActDim.Observability/README.md`
- `Tests/Observability.Tests/ObservabilityTests.cs`
