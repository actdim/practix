# Context

Current state snapshot of `actdim/practix` (.NET).

## Current Solution Architecture

### Autonomous Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`**: Roslyn-based C# script engine (`ScriptEngine`), template interpolation compiler (`Interpolator`), string extension helper (`template.Interpolate(input)`), with `@params` property binding. [Packable NuGet Package w/ README]
- **`ActDim.Reflectron`**: High-performance reflection engine (`TypeAccess`, compiled expression tree property getters/setters, fast dynamic delegates). [Packable NuGet Package w/ README]
- **`ActDim.Three`**: 3D graphics engine math, geometry, materials, lighting, scene graph, and JSON scene graph serialization (`ThreeSerializer`). [Packable NuGet Package w/ README]
- **`ActDim.BlobManager`**: Keyed blob storage with sharded physical data store (`FileSystemBlobDataStore`), SQLite registry (`SQLiteBlobRegistry`), TTL expiration, and DI helpers (`AddBlobManager()`). [Packable NuGet Package w/ README]
- **`ActDim.Observability`**: OpenTelemetry-centric telemetry, ambient context management (`IObservabilityContext`, `EventObservabilityBridge`), and `AddEventObservability()` DI helper. [Packable NuGet Package w/ README]

### Framework & Application Assemblies (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`**: Framework base interfaces, contracts, and domain abstractions. [Packable NuGet Package w/ README]
- **`ActDim.Practix.Common`**: Shared utilities, concurrent collection factories (`ConcurrentFactoryDictionary`), compression (`CompressionManager`), caching proxies, and granular Microsoft DI extensions (`AddAmbientContext()`, `AddCompressionManager()`, `AddMemoryCachingProxy()`, `AddDistributedCachingProxy()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Json`**: Dedicated JSON serialization assembly (`CoreJsonSerializer`, custom converters, policies, attributes) leveraging `ActDim.Reflectron` for fast property setters and Microsoft DI extensions (`AddPractixJson()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.DataAccess`**: Data access layer. [Non-Packable Application Assembly]
- **`ActDim.Practix.Service`**: Primary backend service host using standard Microsoft DI (`IServiceCollection`). [Non-Packable Application Assembly]

## Packaging & Dependency Injection Standard
- **NuGet Packaging Standard**: 8 library projects configured for NuGet package creation with centralized `Directory.Build.props` metadata, explicit `<IsPackable>true</IsPackable>`, `<PackageReadmeFile>README.md</PackageReadmeFile>`, and embedded `README.md` files. Internal application/repo projects explicitly set `<IsPackable>false</IsPackable>`.
- **Zero Autofac Dependency**: Solution standardized completely on `Microsoft.Extensions.DependencyInjection` via granular extension methods (`AddAmbientContext`, `AddCompressionManager`, `AddMemoryCachingProxy`, `AddDistributedCachingProxy`, `AddPractixJson`, `AddFileSystemBlobDataStore`, `AddSQLiteBlobRegistry`, `AddBlobManager`, `AddEventObservability`).

## Solution Health & Verification
- **Solution Build & Pack**: 15/15 projects building cleanly. All 8 NuGet packages generate `.nupkg` and `.snupkg` symbol packages with 0 missing README warnings (`dotnet pack ActDim.Practix.sln`).
- **Total Test Suite**: 493 tests passing across 6 test assemblies:
  - `ActDim.Practix.Json.Tests` (101 tests)
  - `ActDim.Practix.Common.Tests` (213 tests)
  - `ActDim.Emitron.Tests` (41 tests)
  - `ActDim.Reflectron.Tests` (42 tests)
  - `ActDim.BlobManager.Tests` (68 tests)
  - `ActDim.Observability.Tests` (28 tests)
- Zero failures.
