---
protocol: along
slug: feat--extract-practix-json-assembly
type: feat
status: done
priority: high
created: 2026-08-17
updated: 2026-08-17
completed: 2026-08-17
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Extract JSON serialization subsystem into ActDim.Practix.Json

## Context
JSON converters, resolvers, attributes, and `StandardJsonSerializer` were located inside `ActDim.Practix.Common/Json/`. To allow direct integration with `ActDim.Reflectron` fast reflection features without circular dependencies, the JSON subsystem was extracted to `ActDim.Practix.Json`.

## Objectives
- Created `ActDim.Practix.Json.csproj` referencing `Abstractions`, `Common`, and `Reflectron`.
- Moved 20 JSON files from `ActDim.Practix.Common/Json/` to `ActDim.Practix.Json/`.
- Updated `StandardJsonSerializer` to directly call `TypeAccess.GetPropertySetter<object, object>(prop)` from `ActDim.Reflectron`.
- Created `JsonModule.cs` for Autofac registration.
- Added `ActDim.Practix.Json` to solution `ActDim.Practix.sln` and updated project references in `Service` and `Common.Tests`.
- Verified 100% test suite passing (467/467 tests passed).
