---
protocol: along
date: 2026-09-21
slug: add-core-json-serializer-di-consolidation
agent: antigravity
branch: main
commit: pending
summary: Consolidated JSON DI registration into AddCoreJsonSerializer registering all four serialization abstractions
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [json-di-registration-missing-interfaces]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: AddCoreJsonSerializer DI Registration Consolidation

## Overview
Consolidated redundant DI registration methods in `ActDim.Practix.Json` into a single canonical `AddCoreJsonSerializer(this IServiceCollection services)` method, registering `CoreJsonSerializer` as a shared singleton for `IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, and `IStreamSerializer`.

## Accomplishments
1. **Consolidated Registration**:
   - Replaced redundant `AddPractixJson()` and `AddJsonSerializer()` in `ActDim.Practix.Json/Extensions/ServiceCollectionExtensions.cs` with `AddCoreJsonSerializer()`.
   - Registered shared singleton forwarding for `IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, and `IStreamSerializer`.
2. **Updated Call Sites**:
   - Updated `ActDim.Practix.Service/Extensions/ServiceCollectionExtensions.cs` to call `AddCoreJsonSerializer()`.
   - Updated `ActDim.Practix.Json/README.md` and `ActDim.Practix.Json/AGENTS.md`.
3. **Automated Verification**:
   - Added unit test suite in `Tests/Json.Tests/ServiceCollectionExtensionsTests.cs` validating shared singleton resolution for all four interfaces and existing registration preservation.
   - All 720 automated tests passed across the solution.

