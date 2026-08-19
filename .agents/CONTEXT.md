# Context

Current state snapshot of `actdim/practix` (.NET).

## Current Solution Architecture

### Autonomous Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`**: Roslyn-based C# script engine (`ScriptEngine`), template interpolation compiler (`Interpolator`), string extension helper (`template.Interpolate(input)`), with `@params` property binding. [Packable NuGet Package w/ README]
- **`ActDim.Reflectron`**: High-performance reflection engine (`Reflectron`, `IReflectron<T>`, `obj.Reflect()`, indexer access, compiled expression tree property/field getters/setters, weak-referenced instances, fast dynamic delegates). [Packable NuGet Package w/ README]
- **`ActDim.Three`**: 3D graphics engine math, geometry, materials, lighting, scene graph, and JSON scene graph serialization (`ThreeSerializer`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath`**: Core blob engine abstractions (`IBlobManager`, `IBlobDataStore`, `IBlobRegistry`), models (`BlobRecord`, `BlobResult`), multi-datastore `KeyPrefix` routing, and fluent DI builder (`AddBlobManager()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.FileSystemStore`**: Sharded physical data store (`FileSystemBlobDataStore`), key prefix support, hashing, stream pumping, and DI extensions (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.SqliteRegistry`**: SQLite-backed ACID registry (`SQLiteBlobRegistry`), distributed locking, TTL expiration, and DI extension (`WithSQLiteRegistry()`, `AddSQLiteBlobRegistry()`). [Packable NuGet Package w/ README]
- **`ActDim.Observability`**: OpenTelemetry-centric telemetry, ambient context management (`IObservabilityContext`, `EventObservabilityBridge`), and `AddEventObservability()` DI helper. [Packable NuGet Package w/ README]

### Framework & Application Assemblies (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`**: Framework base interfaces, contracts, domain abstractions, design patterns (`IProvider`, `IFactory`, `IBuilder`, `ICommand`, `IHandler`, `ISpecification`), and exceptions (`DataFormatException`, `IncompleteDataException` in `ActDim.Practix.Abstractions.Exceptions`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Common`**: Shared utilities, concurrent collection factories (`ConcurrentFactoryDictionary`), compression (`CompressionManager`), caching proxies, and granular Microsoft DI extensions (`AddAmbientContext()`, `AddCompressionManager()`, `AddMemoryCachingProxy()`, `AddDistributedCachingProxy()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Json`**: Dedicated autonomous JSON serialization assembly (`CoreJsonSerializer`, custom converters, policies, attributes) using compiled Expression Trees for zero-allocation `CopyOptions` and property setters (`AddPractixJson()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.DataAccess`**: Data access layer. [Non-Packable Application Assembly]
- **`ActDim.Practix.Service`**: Primary backend service host using standard Microsoft DI (`IServiceCollection`), including API response envelopes (`BaseApiResult`, `ApiResult` in `ActDim.Practix.Service.Api`). [Non-Packable Application Assembly]

## Packaging, DI & Extensions Standards
- **NuGet Packaging Standard**: 10 library projects configured for NuGet package creation with centralized `Directory.Build.props` metadata, explicit `<IsPackable>true</IsPackable>`, `<PackageReadmeFile>README.md</PackageReadmeFile>`, and embedded `README.md` files. Internal application/repo projects explicitly set `<IsPackable>false</IsPackable>`.
- **Zero Autofac Dependency & Microsoft DI**: Solution standardized completely on `Microsoft.Extensions.DependencyInjection` via granular extension methods placed under `namespace Microsoft.Extensions.DependencyInjection` in `Extensions/` subfolders (`AddAmbientContext`, `AddCompressionManager`, `AddMemoryCachingProxy`, `AddDistributedCachingProxy`, `AddPractixJson`, `AddFileSystemBlobDataStore`, `AddSQLiteBlobRegistry`, `AddBlobManager`, `AddEventObservability`, `AddAppRegistryRepo`, `AddAppRegistryService`, `AddCoreService`, `AddApiGen`).
- **Extensions Organization**: All extension classes live in `Extensions/` folders with namespaces aligned to target extended types (`Ardalis.GuardClauses`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.IO`, `ActDim.Three.Core`).

## Solution Health & Verification
- **Solution Build & Pack**: All projects in `ActDim.Practix.sln` and `ActDim.Three.sln` build cleanly with 0 errors and 0 warnings.
- **Total Test Suite**: 550+ tests passing across test assemblies (222 in Common.Tests, 102 in Json.Tests, 56 in Reflectron.Tests, 54 in Emitron.Tests, 74 in BytePath.Tests, 28 in Observability.Tests, 35 in Three.Tests) with zero failures.
