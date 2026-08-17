---
slug: task--configure-nuget-packaging
type: task
status: in-progress
priority: high
created: 2026-08-17
updated: 2026-08-17
---

# Configure 6 ActDim projects for NuGet package publishing

## Context
6 library projects in the solution (`ActDim.Practix.Abstractions`, `ActDim.Practix.Common`, `ActDim.Practix.Json`, `ActDim.Reflectron`, `ActDim.Emitron`, `ActDim.Three`) are designated for publication to NuGet. Centralized metadata and MSBuild packaging properties need to be configured.

## Objectives
- Create centralized root `Directory.Build.props` with metadata, license, symbols, and XML doc generation settings.
- Update `.csproj` files for the 6 target projects with `<IsPackable>true</IsPackable>`, `<PackageId>`, `<Description>`, and `<PackageTags>`.
- Run `dotnet pack --configuration Release --output ./nupkgs` and verify 6 generated `.nupkg` packages.
- Verify 100% test suite passing.
