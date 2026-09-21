---
protocol: along
date: 2026-09-21
slug: decouple-recyclable-memorystream-from-abstractions
agent: antigravity
branch: main
commit: pending
summary: Decoupled Microsoft.IO.RecyclableMemoryStream from ActDim.Practix.Abstractions into ActDim.Practix.Common
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [debt--move-recyclable-memory-stream-extensions-to-common]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Decouple RecyclableMemoryStream from ActDim.Practix.Abstractions

## Overview
Extracted concrete `Microsoft.IO.RecyclableMemoryStreamManager` extension methods from `ActDim.Practix.Abstractions` into `ActDim.Practix.Common` (`AmbientContextMemoryExtensions.cs`), removing the third-party dependency from `ActDim.Practix.Abstractions.csproj`.

## Accomplishments
1. **Abstractions Decoupling**:
   - Removed `GetMemoryManager`, `WithMemoryManager`, and `WithMemory` from `ActDim.Practix.Abstractions/Context/AmbientContextExtensions.cs`.
   - Removed `<PackageReference Include="Microsoft.IO.RecyclableMemoryStream" />` from `ActDim.Practix.Abstractions/ActDim.Practix.Abstractions.csproj`.
   - Updated XML doc comments on `AmbientKeys.MemoryManager` to eliminate external cref warnings.
2. **Common Extension Extraction**:
   - Created `AmbientContextMemoryExtensions.cs` in `ActDim.Practix.Common/Context/` providing typed `GetMemoryManager()`, `WithMemoryManager()`, and `WithMemory()` for `IAmbientContext`.
3. **Verification**:
   - Rebuilt solution and verified 717 automated tests pass with zero failures.

