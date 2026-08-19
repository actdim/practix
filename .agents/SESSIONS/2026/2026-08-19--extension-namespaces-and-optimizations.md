---
date: 2026-08-19
slug: extension-namespaces-and-optimizations
agent: Antigravity / Gemini 3.7 Flash
branch: main
commit: 630e7e8
summary: Standardized extension methods into Extensions/ folders across all projects, unified DI registrations under Microsoft.Extensions.DependencyInjection, aligned target type namespaces, and optimized EnumerableExtensions and FuncExtensions
---

# Extension Namespaces Standardization & Performance Optimizations

## What Changed & Why
1. **Extensions Folder Layout**:
   - Reorganized all extension classes into dedicated `Extensions/` subfolders within each project.
   - Extracted `SceneDocumentExtensions` and `TypedArrayWriteExtensions` into separate files under `ActDim.Three/Extensions/`.
   - Relocated `JsonElementExtensions` to `ActDim.Practix.Json/Extensions/`.
   - Relocated `ApiExtensions`, `TypeExtensions`, and `OpenApiServiceCollectionExtensions` to `ActDim.Practix.Service/Extensions/`.
   - Relocated `EntityRefExtensions` to `AppRegistry/AppRegistry.Domain/Extensions/`.

2. **DI Registration Namespaces (`Microsoft.Extensions.DependencyInjection`)**:
   - Standardized all `IServiceCollection` extension methods across `ActDim.BytePath`, `ActDim.BytePath.FileSystemStore`, `ActDim.BytePath.SqliteRegistry`, `ActDim.Observability`, `ActDim.Practix.Common`, `ActDim.Practix.Json`, `ActDim.Practix.Service`, `AppRegistry.Repo`, and `AppRegistry.Service` into `namespace Microsoft.Extensions.DependencyInjection`.

3. **Target Type Alignment**:
   - `GuardExtensions` -> `namespace Ardalis.GuardClauses`
   - `MemoryCacheExtensions` -> `namespace Microsoft.Extensions.Caching.Memory`
   - `MemoryStreamManagerExtensions` -> `namespace Microsoft.IO`
   - `SceneDocumentExtensions` -> `namespace ActDim.Three.Core`
   - `TypedArrayWriteExtensions` -> `namespace ActDim.Three.Core.Buffers`

4. **EnumerableExtensions & FuncExtensions Optimizations**:
   - Cleaned dead commented-out code blocks (`PartitionHelper`, legacy `Zip`, `ToHashSet`, `AsDuckEnumerable`, `Traverse`).
   - Rewrote `Partition<T>` on top of .NET's native `source.Chunk(size)`.
   - Optimized `MinOrDefault` and `MaxOrDefault` to single-pass iterations.
   - Added fast-paths for `Count == 0` on `ICollection<T>` / `IReadOnlyCollection<T>` in `IsNullOrEmpty`.
   - Replaced blocking `FactoryDictionary` (with `ReaderWriterLockSlim` write lock and GC finalizer) and race-prone `syncMap` in `FuncExtensions.Memoize` with lock-free `ConcurrentFactoryDictionary`.
   - Added `IEqualityComparer<TKey>` constructor overload in `ConcurrentFactoryDictionary`.

5. **Unit Testing**:
   - Added `EnumerableExtensionsTests.cs` (6 unit tests).
   - Added `FuncExtensionsTests.cs` (3 unit tests).

## Files Touched
- `ActDim.Three/Extensions/SceneDocumentExtensions.cs`
- `ActDim.Three/Extensions/TypedArrayWriteExtensions.cs`
- `ActDim.Three/Serialization/SceneDocument.cs`
- `ActDim.BytePath/Extensions/BlobManagerServiceCollectionExtensions.cs`
- `ActDim.BytePath.FileSystemStore/Extensions/FileSystemBlobDataStoreExtensions.cs`
- `ActDim.BytePath.SqliteRegistry/Extensions/SQLiteBlobRegistryExtensions.cs`
- `ActDim.Observability/Extensions/EventObservabilityExtensions.cs`
- `ActDim.Practix.Common/Extensions/ServiceCollectionExtensions.cs`
- `ActDim.Practix.Common/Extensions/GuardExtensions.cs`
- `ActDim.Practix.Common/Extensions/MemoryCacheExtensions.cs`
- `ActDim.Practix.Common/Extensions/MemoryStreamManagerExtensions.cs`
- `ActDim.Practix.Common/Extensions/EnumerableExtensions.cs`
- `ActDim.Practix.Common/Extensions/FuncExtensions.cs`
- `ActDim.Practix.Common/Extensions/StringExtensions.cs`
- `ActDim.Practix.Common/Collections/Concurrent/ConcurrentFactoryDictionary.cs`
- `ActDim.Practix.Json/Extensions/ServiceCollectionExtensions.cs`
- `ActDim.Practix.Json/Extensions/JsonElementExtensions.cs`
- `ActDim.Practix.Service/Extensions/ServiceCollectionExtensions.cs`
- `ActDim.Practix.Service/Extensions/OpenApiServiceCollectionExtensions.cs`
- `ActDim.Practix.Service/Extensions/ApiExtensions.cs`
- `ActDim.Practix.Service/Extensions/TypeExtensions.cs`
- `ActDim.Practix.Service/CoreService.cs`
- `AppRegistry/AppRegistry.Repo/Extensions/ServiceCollectionExtensions.cs`
- `AppRegistry/AppRegistry.Service/Extensions/ServiceCollectionExtensions.cs`
- `AppRegistry/AppRegistry.Domain/Extensions/EntityRefExtensions.cs`
- `Tests/Common.Tests/Extensions/EnumerableExtensionsTests.cs`
- `Tests/Common.Tests/Extensions/FuncExtensionsTests.cs`

## Decisions
- Recorded ADR-014 in `.agents/DECISIONS.md`.
