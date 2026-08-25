# Context

Current state snapshot of `actdim/practix` (.NET).

## Current Solution Architecture

### Autonomous Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`**: Roslyn-based C# script engine (`ScriptEngine`), template interpolation compiler (`Interpolator`), string extension helper (`template.Interpolate(input)`), with `@params` property binding. [Packable NuGet Package w/ README]
- **`ActDim.Reflectron`**: High-performance reflection engine (`Reflectron`, `IReflectron<T>`, `obj.Reflect()`, indexer access, compiled expression tree property/field getters/setters, weak-referenced instances, fast dynamic delegates). [Packable NuGet Package w/ README]
- **`ActDim.Three`**: 3D graphics engine math, geometry, materials, lighting, scene graph, and JSON scene graph serialization (`ThreeSerializer`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath`**: Core blob engine implementation, models (`BlobRecord`, `BlobResult`), multi-datastore `KeyPrefix` routing, and fluent DI builder (`AddBlobManager()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.FileSystemStore`**: Sharded physical data store (`FileSystemBlobDataStore`), key prefix support, hashing, stream pumping, and DI extensions (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.SqliteRegistry`**: SQLite-backed ACID registry (`SQLiteBlobRegistry`), distributed locking, TTL expiration, and DI extension (`WithSQLiteRegistry()`, `AddSQLiteBlobRegistry()`). [Packable NuGet Package w/ README]
- **`ActDim.Observability`**: OpenTelemetry-centric telemetry, ambient context integration (`IObservabilityContext`, `EventObservabilityBridge`), unified `ObservabilityStatus` struct (`Name`, `Progress`, `Icon`, `Step`, `TotalSteps`), `observability.Status` getter, single `"status"` data key, and `AddEventObservability()` DI helper. [Packable NuGet Package w/ README]

### Framework & Application Assemblies (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`**: Framework base interfaces, contracts, domain abstractions, storage interfaces (`IBlobManager`, `IBlobDataStore`, `IBlobRegistry`), design patterns (`IProvider`, `IFactory`, `IBuilder`, `ICommand`, `IHandler`, `ISpecification`), ambient keys (`AmbientKeys`), typed ambient extensions (`AmbientContextExtensions`), and exceptions (`DataFormatException`, `IncompleteDataException` in `ActDim.Practix.Abstractions.Exceptions`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Common`**: Shared utilities, concurrent collection factories (`ConcurrentFactoryDictionary`), compression (`CompressionManager`), caching proxies, and ambient context (`AmbientContext` owning `AsyncLocal`, `AddAmbientContext()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Json`**: Dedicated autonomous JSON serialization assembly (`CoreJsonSerializer`, custom converters, policies, attributes) using compiled Expression Trees for zero-allocation `CopyOptions` and property setters (`AddPractixJson()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.DataAccess`**: Data access layer. [Non-Packable Application Assembly]
- **`ActDim.Practix.Service`**: Primary backend service host using standard Microsoft DI (`IServiceCollection`), including API response envelopes (`BaseApiResult`, `ApiResult` in `ActDim.Practix.Service.Api`). [Non-Packable Application Assembly]

## Packaging, Dependency & Architecture Standards
- **Central Package Management (CPM)**: Repository fully standardized on NuGet CPM via root `Directory.Packages.props` (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`), declaring versions for all 42 dependencies centrally and removing inline `Version="..."` from all 21 `.csproj` files.
- **Ambient Context Management**: `AmbientContext` acts as the direct holder of `AsyncLocal<ImmutableDictionary<string, object>>` and singleton implementation of `IAmbientContext`. Static facade delegates 1-to-1 to `Current` and `AmbientContextExtensions` for scoped `Services`, `User`, `CancellationToken`, `Blobs`, `Compression`, `LoggerFactory`, and zero-DI `Log<T>()`.
- **Nullable Annotations Standard**: Solution-wide adoption of `<Nullable>annotations</Nullable>` across all projects in `ActDim.Practix.sln` and `ActDim.Three.sln`.
- **NuGet Packaging Standard**: Packable library projects configured for NuGet package creation with centralized metadata (Version 1.0.8), explicit `<IsPackable>true</IsPackable>`, `<PackageReadmeFile>README.md</PackageReadmeFile>`, and embedded `README.md` files. Internal application/repo projects explicitly set `<IsPackable>false</IsPackable>`.
- **Zero Autofac Dependency & Microsoft DI**: Solution standardized completely on `Microsoft.Extensions.DependencyInjection` via granular extension methods placed under `namespace Microsoft.Extensions.DependencyInjection` in `Extensions/` subfolders.
- **Extensions Organization**: All extension classes live in `Extensions/` folders with namespaces aligned to target extended types (`Ardalis.GuardClauses`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.IO`, `ActDim.Three.Core`).

## Solution Health & Verification
- **Solution Build & Pack**: All projects in `ActDim.Practix.sln` and `ActDim.Three.sln` build cleanly with 0 errors and 0 warnings.
- **Total Test Suite**: 560 tests passing in `ActDim.Practix.sln` (238 in Common.Tests, 102 in Json.Tests, 56 in Reflectron.Tests, 54 in Emitron.Tests, 79 in BytePath.Tests, 31 in Observability.Tests) plus 35 in `ActDim.Three.sln` with zero failures.
- **Observability Testing & Developer Tooling**: Integrated NDJSON/CLEF ingestion, OpenTelemetry OTLP compatibility, and log search testing in `Tests/Observability.Tests` for **VictoriaLogs**, **OpenObserve**, and **Seq** with process auto-launching from `Tools/`.
