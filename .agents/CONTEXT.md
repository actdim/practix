# Context

Current state snapshot of `actdim/practix` (.NET).

## Current Solution Architecture

### Autonomous Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`**: Roslyn-based C# script engine (`ScriptEngine`), template interpolation compiler (`Interpolator`), string extension helper (`template.Interpolate(input)`), with `@params` property binding. [Packable NuGet Package w/ README]
- **`ActDim.Reflectron`**: High-performance reflection engine (`TypeAccess`, compiled expression tree property getters/setters, fast dynamic delegates). [Packable NuGet Package w/ README]
- **`ActDim.Three`**: 3D graphics engine math, geometry, materials, lighting, scene graph, and JSON scene graph serialization (`ThreeSerializer`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath`**: Core blob engine abstractions (`IBlobManager`, `IBlobDataStore`, `IBlobRegistry`), models (`BlobRecord`, `BlobResult`), and fluent DI builder (`AddBlobManager()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.FileSystemStore`**: Sharded physical data store (`FileSystemBlobDataStore`), hashing, stream pumping, and DI extension (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.SqliteRegistry`**: SQLite-backed ACID registry (`SQLiteBlobRegistry`), distributed locking, TTL expiration, and DI extension (`WithSQLiteRegistry()`, `AddSQLiteBlobRegistry()`). [Packable NuGet Package w/ README]
- **`ActDim.Observability`**: OpenTelemetry-centric telemetry, ambient context management (`IObservabilityContext`, `EventObservabilityBridge`), and `AddEventObservability()` DI helper. [Packable NuGet Package w/ README]

### Framework & Application Assemblies (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`**: Framework base interfaces, contracts, domain abstractions, design patterns (`IProvider`, `IFactory`, `IBuilder`, `ICommand`, `IHandler`, `ISpecification`), and exceptions (`DataFormatException`, `IncompleteDataException` in `ActDim.Practix.Abstractions.Exceptions`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Common`**: Shared utilities, concurrent collection factories (`ConcurrentFactoryDictionary`), compression (`CompressionManager`), caching proxies, and granular Microsoft DI extensions (`AddAmbientContext()`, `AddCompressionManager()`, `AddMemoryCachingProxy()`, `AddDistributedCachingProxy()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Json`**: Dedicated JSON serialization assembly (`CoreJsonSerializer`, custom converters, policies, attributes) leveraging `ActDim.Reflectron` for fast property setters and Microsoft DI extensions (`AddPractixJson()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.DataAccess`**: Data access layer. [Non-Packable Application Assembly]
- **`ActDim.Practix.Service`**: Primary backend service host using standard Microsoft DI (`IServiceCollection`), including API response envelopes (`BaseApiResult`, `ApiResult` in `ActDim.Practix.Service.Api`). [Non-Packable Application Assembly]

## Packaging & Dependency Injection Standard
- **NuGet Packaging Standard**: 10 library projects configured for NuGet package creation with centralized `Directory.Build.props` metadata, explicit `<IsPackable>true</IsPackable>`, `<PackageReadmeFile>README.md</PackageReadmeFile>`, and embedded `README.md` files. Internal application/repo projects explicitly set `<IsPackable>false</IsPackable>`.
- **Zero Autofac Dependency**: Solution standardized completely on `Microsoft.Extensions.DependencyInjection` via granular extension methods (`AddAmbientContext`, `AddCompressionManager`, `AddMemoryCachingProxy`, `AddDistributedCachingProxy`, `AddPractixJson`, `AddFileSystemBlobDataStore`, `AddSQLiteBlobRegistry`, `AddBlobManager`, `AddEventObservability`).

## Solution Health & Verification
- **Solution Build & Pack**: 17/17 projects building cleanly. All 10 NuGet packages generate `.nupkg` and `.snupkg` symbol packages with 0 missing README warnings (`dotnet pack ActDim.Practix.sln`).
- **Total Test Suite**: 494 tests passing across 6 test assemblies:
  - `ActDim.Practix.Json.Tests` (101 tests)
  - `ActDim.Practix.Common.Tests` (213 tests)
  - `ActDim.Emitron.Tests` (41 tests)
  - `ActDim.Reflectron.Tests` (42 tests)
  - `ActDim.BytePath.Tests` (69 tests)
  - `ActDim.Observability.Tests` (28 tests)
- Zero failures.
