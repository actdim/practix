---
protocol: along
date: 2026-08-17
slug: extract-practix-json-assembly
agent: antigravity
branch: main
commit: head
summary: Extracted the entire JSON serialization subsystem from ActDim.Practix.Common into a standalone assembly ActDim.Practix.Json with a direct reference to ActDim.Reflectron.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: Extract ActDim.Practix.Json Assembly

## Changes Made & Rationale
- **Created `ActDim.Practix.Json/ActDim.Practix.Json.csproj`**:
  - New assembly referencing `ActDim.Practix.Abstractions`, `ActDim.Practix.Common`, and `ActDim.Reflectron`.
  - Solved architectural layering: `Abstractions` $\leftarrow$ `Common` $\leftarrow$ `Reflectron` $\leftarrow$ `Json`.
- **Moved JSON Subsystem Files**:
  - Moved 20 JSON files (converters, resolvers, policies, attributes, `StandardJsonSerializer`) from `ActDim.Practix.Common/Json/` to `ActDim.Practix.Json/`.
  - Added `JsonModule.cs` for Autofac registration.
- **Refactored `StandardJsonSerializer`**:
  - Directly uses `TypeAccess.GetPropertySetter<object, object>(prop)` from `ActDim.Reflectron`.
- **Solution & Dependencies**:
  - Registered `ActDim.Practix.Json.csproj` in `ActDim.Practix.sln`.
  - Added project reference to `ActDim.Practix.Json` in `ActDim.Practix.Service.csproj` and `ActDim.Practix.Common.Tests.csproj`.

## Files Touched
- `ActDim.Practix.Json/ActDim.Practix.Json.csproj`
- `ActDim.Practix.Json/JsonModule.cs`
- `ActDim.Practix.Json/StandardJsonSerializer.cs` (and 19 moved JSON files)
- `ActDim.Practix.Common/CommonModule.cs`
- `ActDim.Practix.Service/ActDim.Practix.Service.csproj`
- `Tests/Common.Tests/ActDim.Practix.Common.Tests.csproj`
- `ActDim.Practix.sln`

## Verification
- Executed `dotnet test ActDim.Practix.sln`.
- All 467 tests across 5 test assemblies passed cleanly (0 failures, 0 errors).
