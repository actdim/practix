---
protocol: along
slug: align-product-and-test-assembly-namespaces
date: 2026-09-24
agent: antigravity
summary: "Align Product and Test Assembly Namespaces"
---

# Session: Align Product and Test Assembly Namespaces
Date: 2026-09-24
Issue: `debt--align-product-and-test-assembly-namespaces`

## Objectives
Align namespaces across secondary product assemblies and test suites to match project names and physical folder layouts:
1. `ActDim.BytePath.FileSystemStore`: `FileSystemBlobDataStore.cs` & `FileSystemBlobDataStoreOptions.cs` aligned to `ActDim.BytePath.FileSystemStore`.
2. `ActDim.BytePath.SqliteRegistry`: `SQLiteBlobRegistry.cs`, `SQLiteBlobRegistryOptions.cs`, and `RepoDbSqLiteBootstrapper.cs` aligned to `ActDim.BytePath.SqliteRegistry`.
3. `ActDim.BytePath`: `BlobManagerBuilder.cs` and `IBlobManagerBuilder.cs` in `Extensions/` aligned to `ActDim.BytePath.Extensions`.
4. `ActDim.Practix.RepoDb`: `Sql/Sql.cs` aligned to `ActDim.Practix.RepoDb.Sql`.
5. `ActDim.Practix.Service`: `Claims/RegisteredClaimNames.cs` aligned to `ActDim.Practix.Service.Claims`.
6. `ActDim.Three`:
   - `Scenes/Metadata.cs` and `Scenes/Scene.cs` aligned to `ActDim.Three.Scenes`.
   - `Serialization/SceneDocument.cs` and `Serialization/ThreeSerializer.cs` aligned to `ActDim.Three.Serialization`.
7. Consumers and test suites updated across `BytePath.Tests`, `RepoDb.Tests`, `Service.Tests`, `AppRegistry.Tests`, `Three.Tests`, and `ActDim.Three.NewtonsoftJson`.

## Changes Made
- FileSystemBlobDataStore.cs and FileSystemBlobDataStoreOptions.cs: namespace set to `ActDim.BytePath.FileSystemStore`.
- FileSystemBlobDataStoreExtensions.cs: added `using ActDim.BytePath.FileSystemStore;` and `using ActDim.BytePath.Extensions;`.
- SQLiteBlobRegistry.cs, SQLiteBlobRegistryOptions.cs, and RepoDbSqLiteBootstrapper.cs: namespace set to `ActDim.BytePath.SqliteRegistry`.
- SQLiteBlobRegistryExtensions.cs: added `using ActDim.BytePath.SqliteRegistry;` and `using ActDim.BytePath.Extensions;`.
- BlobManagerBuilder.cs and IBlobManagerBuilder.cs: namespace set to `ActDim.BytePath.Extensions`.
- BlobManagerServiceCollectionExtensions.cs: added `using ActDim.BytePath.Extensions;`.
- Sql.cs: namespace set to `ActDim.Practix.RepoDb.Sql`, removed redundant inner using.
- RegisteredClaimNames.cs: namespace set to `ActDim.Practix.Service.Claims`.
- AppContext.cs: added `using ActDim.Practix.Service.Claims;`.
- Metadata.cs and Scene.cs: namespace set to `ActDim.Three.Scenes`.
- SceneDocument.cs and ThreeSerializer.cs: namespace set to `ActDim.Three.Serialization`.
- SceneDocumentExtensions.cs and SceneDocumentStjConverter.cs: added necessary using statements for `ActDim.Three.Scenes` and `ActDim.Three.Serialization`.
- SceneDocumentConverter.cs and ThreeNewtonsoftSerializer.cs: added `using ActDim.Three.Scenes;` and `using ActDim.Three.Serialization;`.
- Updated test suites across `Tests/BytePath.Tests/`, `Tests/Service.Tests/`, and `Tests/Three.Tests/`.

## Verification
- Executed `dotnet test ActDim.Practix.sln -v q` via `.along/scripts/test.py`:
  - 12 test projects executed.
  - 732 passed, 0 failed, 0 skipped.
