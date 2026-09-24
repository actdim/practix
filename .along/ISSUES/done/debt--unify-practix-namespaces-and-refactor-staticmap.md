---
protocol: along
protocol_version: "2.2.26"
slug: debt--unify-practix-namespaces-and-refactor-staticmap
type: debt
status: done
priority: high
created: 2026-09-24
updated: 2026-09-24
completed: 2026-09-24
agent: antigravity
tags: [architecture, namespace-unification, collections, staticmap, technical-debt]
milestone: v2.0.0-along-transition
blocked_by: []
---

# Unify Practix Namespaces and Relocate StaticMap

## Overview
1. Set RootNamespace in `ActDim.Practix.Common.csproj` to `ActDim.Practix`.
2. Unified all namespaces across `ActDim.Practix.Common` under `ActDim.Practix.<Feature>` (dropping the `.Common.` segment: Memory, Introspection, DataFormat, Runtime, Threading, Compression, root).
3. Relocated `StaticMap` from `ActDim.Practix.Common/Runtime/StaticMap.cs` to `ActDim.Practix.Common/Collections/Specialized/StaticMap.cs` in namespace `ActDim.Practix.Collections.Specialized`.
4. Deleted legacy unused `StaticStringDictionary.cs` and cleaned up empty folders.
5. Updated tests and consuming references across the solution.
6. Verified full test suite passes cleanly with zero regressions.

## Requirements
- Update `ActDim.Practix.Common.csproj` with `<RootNamespace>ActDim.Practix</RootNamespace>`.
- Remove `StaticStringDictionary.cs` from `Collections/Generic/Advanced/`.
- Move `StaticMap.cs` to `Collections/Specialized/StaticMap.cs` with namespace `ActDim.Practix.Collections.Specialized`.
- Align all namespaces in `ActDim.Practix.Common` to `ActDim.Practix.*`.
- Update all affected tests in `Tests/Common.Tests/` and other referencing assemblies.
- Run tests and verify zero regressions.
