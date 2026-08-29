---
protocol: along
date: 2026-08-17
slug: rename-core-json-serializer
agent: antigravity
branch: main
commit: head
summary: Renamed StandardJsonSerializer to CoreJsonSerializer across ActDim.Practix.Json and test assemblies.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: Rename StandardJsonSerializer to CoreJsonSerializer

## Changes Made & Rationale
- **Renamed Serializer**:
  - Renamed `StandardJsonSerializer.cs` $\rightarrow$ `CoreJsonSerializer.cs` and class name `StandardJsonSerializer` $\rightarrow$ `CoreJsonSerializer` in `ActDim.Practix.Json`.
  - Renamed `StandardJsonSerializerTests.cs` $\rightarrow$ `CoreJsonSerializerTests.cs` and class name in `ActDim.Practix.Json.Tests`.
  - Updated all registration methods (`AddPractixJson()`, `AddJsonSerializer()`) and call sites across the solution.

## Files Touched
- `ActDim.Practix.Json/CoreJsonSerializer.cs` (renamed from `StandardJsonSerializer.cs`)
- `ActDim.Practix.Json/Extensions/ServiceCollectionExtensions.cs`
- `ActDim.Practix.Json/JsonSerializerServiceExtensions.cs`
- `Tests/Json.Tests/CoreJsonSerializerTests.cs` (renamed from `StandardJsonSerializerTests.cs`)
- `Tests/Json.Tests/JsonNamingAttributeTests.cs`

## Verification
- Executed `dotnet test ActDim.Practix.sln`.
- All 484 tests across 6 test assemblies passed cleanly (0 failures, 0 errors).
