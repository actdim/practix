---
protocol: along
protocol_version: "2.2.26"
slug: debt--align-product-and-test-assembly-namespaces
type: debt
status: done
priority: high
created: 2026-09-24
updated: 2026-09-24
completed: 2026-09-24
agent: antigravity
tags: [architecture, namespace-alignment, bytepath, repodb, service, three, technical-debt]
milestone: v2.0.0-along-transition
blocked_by: []
---

# Align Product and Test Assembly Namespaces

## Overview
Align namespaces across product assemblies and test assemblies to match their project and folder structure:
1. `ActDim.BytePath.FileSystemStore`: `FileSystemBlobDataStore.cs` & `FileSystemBlobDataStoreOptions.cs` to `ActDim.BytePath.FileSystemStore`.
2. `ActDim.BytePath.SqliteRegistry`: `SQLiteBlobRegistry.cs`, `SQLiteBlobRegistryOptions.cs`, `RepoDbSqLiteBootstrapper.cs` to `ActDim.BytePath.SqliteRegistry`.
3. `ActDim.BytePath`: `Extensions/BlobManagerBuilder.cs` and `Extensions/IBlobManagerBuilder.cs` to `ActDim.BytePath.Extensions`.
4. `ActDim.Practix.RepoDb`: `Sql/Sql.cs` to `ActDim.Practix.RepoDb.Sql`.
5. `ActDim.Practix.Service`: `Claims/RegisteredClaimNames.cs` to `ActDim.Practix.Service.Claims`.
6. `ActDim.Three`:
   - `Scenes/Metadata.cs` and `Scenes/Scene.cs` to `ActDim.Three.Scenes`.
   - `Serialization/SceneDocument.cs` and `Serialization/ThreeSerializer.cs` to `ActDim.Three.Serialization`.
7. `Tests/Json.Tests`: verify all test files use `ActDim.Practix.Json.Tests`.
8. Update all referencing projects and test suites, ensuring 100% test pass rate.

## Requirements
- Update namespaces in target source files.
- Update using directives in consumers, tests, and dependent projects.
- Run full test suite and verify 0 regressions.
