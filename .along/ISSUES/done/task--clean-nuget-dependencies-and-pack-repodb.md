---
protocol: along
protocol_version: "2.2.26"
slug: task--clean-nuget-dependencies-and-pack-repodb
type: task
status: done
completed: 2026-09-21
priority: high
created: 2026-09-21
updated: 2026-09-21
agent: antigravity
tags: [nuget, packaging, repodb, dependencies, clean-up, guard-clauses]
related: []
milestone: v2.0.0-along-transition
blocked_by: []
---

# Clean NuGet Dependencies, Remove GuardClauses, and Pack ActDim.Practix.RepoDb

## Overview
Purge unused dependencies from NuGet-published libraries, eliminate `Ardalis.GuardClauses` across all packable projects in favor of modern standard .NET 10 throw helpers (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero`), remove `System.Runtime.Caching` from `ActDim.Practix.Common`, and configure `ActDim.Practix.RepoDb` as a clean, packable NuGet package.

## Requirements
1. In `ActDim.Practix.RepoDb`:
   - Set `<IsPackable>true</IsPackable>`.
   - Remove `<PackageReference Include="Ardalis.GuardClauses" />`.
   - Remove `<ProjectReference Include="..\ActDim.Practix.Abstractions\..." />`.
   - Remove `<ProjectReference Include="..\ActDim.Practix.Common\..." />`.
2. In `ActDim.Practix.Common`:
   - Remove `<PackageReference Include="System.Runtime.Caching" />`.
   - Remove `<PackageReference Include="Ardalis.GuardClauses" />`.
   - Replace all `Guard.Against.*` calls with standard modern BCL throw helpers.
   - Remove or adapt `GuardExtensions.cs`.
3. In `ActDim.Reflectron`:
   - Replace all `Guard.Against.*` calls with standard modern BCL throw helpers.
4. In `ActDim.Emitron`:
   - Remove `<PackageReference Include="Ardalis.GuardClauses" />`.
   - Replace all `Guard.Against.*` calls with standard modern BCL throw helpers.
5. In `ActDim.Emitron.Razor`:
   - Remove `<PackageReference Include="Ardalis.GuardClauses" />`.
   - Replace all `Guard.Against.*` calls with standard modern BCL throw helpers.
6. In `ActDim.Practix.Json`:
   - Remove unused `<PackageReference Include="Ardalis.GuardClauses" />`.
7. Verify all test suites compile and pass with 0 failures.
