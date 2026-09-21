---
protocol: along
protocol_version: "2.2.26"
slug: debt--move-recyclable-memory-stream-extensions-to-common
type: debt
status: done
priority: medium
created: 2026-09-21
updated: 2026-09-21
completed: 2026-09-21
agent: antigravity
tags: [ambient-context, memory, recyclable-memory-stream, abstractions, decoupling]
related: []
milestone: v2.0.0-along-transition
blocked_by: []
---

# Decouple RecyclableMemoryStreamManager from ActDim.Practix.Abstractions

## Overview
Remove concrete third-party dependency `Microsoft.IO.RecyclableMemoryStream` from `ActDim.Practix.Abstractions` by extracting `RecyclableMemoryStreamManager` ambient context extension methods (`GetMemoryManager`, `WithMemoryManager`, `WithMemory`) into `ActDim.Practix.Common`.

## Requirements
1. Remove `GetMemoryManager`, `WithMemoryManager`, and `WithMemory` from `ActDim.Practix.Abstractions/Context/AmbientContextExtensions.cs`.
2. Remove `<PackageReference Include="Microsoft.IO.RecyclableMemoryStream" />` from `ActDim.Practix.Abstractions/ActDim.Practix.Abstractions.csproj`.
3. Create `AmbientContextMemoryExtensions.cs` in `ActDim.Practix.Common/Context/` implementing `GetMemoryManager`, `WithMemoryManager`, and `WithMemory` for `IAmbientContext`.
4. Verify all builds and existing test suites pass cleanly.

