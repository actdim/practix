# Context

Current state snapshot of `actdim/practix` (.NET).

## Current Solution Architecture

### Autonomous Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`**: Roslyn-based C# script engine (`ScriptEngine`), template interpolation compiler (`Interpolator`), string extension helper (`template.Interpolate(input)`), with `@params` property binding. [Packable NuGet Package w/ README]
- **`ActDim.Reflectron`**: High-performance reflection engine (`Reflectron`, `IReflectron<T>`, `obj.Reflect()`, indexer access, compiled expression tree property/field getters/setters, weak-referenced instances, fast dynamic delegates). [Packable NuGet Package w/ README]
- **`ActDim.Three`**: 3D graphics engine math, geometry, materials, lighting, scene graph, `Layers` bitmask, Instanced/Interleaved buffers, PBR/Shader materials, and native System.Text.Json scene serialization (`ThreeSerializer`, `TypedArray`). Cleanly decoupled from Newtonsoft.Json. [Packable NuGet Package w/ README]
- **`ActDim.Three.NewtonsoftJson`**: Dedicated Newtonsoft.Json compatibility adapter and custom converters (`ThreeNewtonsoftSerializer`, `SceneDocumentConverter`, `BufferAttributeConverter`, `ElementConverter`, `CamelCaseCustomResolver`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath`**: Core blob engine implementation, models (`BlobRecord`, `BlobResult`), multi-datastore `KeyPrefix` routing, and fluent DI builder (`AddBlobManager()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.FileSystemStore`**: Sharded physical data store (`FileSystemBlobDataStore`), key prefix support, hashing, stream pumping, and DI extensions (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`). [Packable NuGet Package w/ README]
- **`ActDim.BytePath.SqliteRegistry`**: SQLite-backed ACID registry (`SQLiteBlobRegistry`), distributed locking, TTL expiration, and DI extension (`WithSQLiteRegistry()`, `AddSQLiteBlobRegistry()`). [Packable NuGet Package w/ README]
- **`ActDim.Observability`**: OpenTelemetry-centric telemetry, ambient context integration (`IObservabilityContext`, `EventObservabilityBridge`), unified `ObservabilityStatus` struct (`Name`, `Progress`, `Icon`, `Step`, `TotalSteps`), `observability.Status` getter, single `"status"` data key, and `AddEventObservability()` DI helper. [Packable NuGet Package w/ README]

### Framework & Application Assemblies (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`**: Framework base interfaces, contracts, domain abstractions, storage interfaces (`IBlobManager`, `IBlobDataStore`, `IBlobRegistry`), design patterns (`IProvider`, `IFactory`, `IBuilder`, `ICommand`, `IHandler`, `ISpecification`), ambient keys (`AmbientKeys`), typed ambient extensions (`AmbientContextExtensions`), and exceptions (`DataFormatException`, `IncompleteDataException` in `ActDim.Practix.Abstractions.Exceptions`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Common`**: Shared utilities, concurrent collection factories (`ConcurrentFactoryDictionary`), compression (`CompressionManager`), caching proxies, and ambient context (`AmbientContext` owning `AsyncLocal`, `AddAmbientContext()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.Json`**: Dedicated autonomous JSON serialization subsystem (`CoreJsonSerializer`, custom converters, policies, attributes) using compiled Expression Trees for zero-allocation `CopyOptions` and property setters (`AddPractixJson()`). [Packable NuGet Package w/ README]
- **`ActDim.Practix.DataAccess`**: Data access layer. [Non-Packable Application Assembly]
- **`ActDim.Practix.Service`**: Primary backend service host using standard Microsoft DI (`IServiceCollection`), including API response envelopes (`BaseApiResult`, `ApiResult` in `ActDim.Practix.Service.Api`). [Non-Packable Application Assembly]

## Packaging, Dependency & Architecture Standards
- **Central Package Management (CPM) & Central Versioning**: Repository fully standardized on NuGet CPM via root `Directory.Packages.props` and central versioning in `Directory.Build.props` (**Version 1.0.9**).
- **Ambient Context Management**: `AmbientContext` acts as the direct holder of `AsyncLocal<ImmutableDictionary<string, object>>` and singleton implementation of `IAmbientContext`. Static facade delegates 1-to-1 to `Current` and `AmbientContextExtensions`.
- **Nullable Annotations Standard**: Solution-wide adoption of `<Nullable>annotations</Nullable>` across all projects in `ActDim.Practix.sln` and `ActDim.Three.sln`.
- **Zero External Legacy JSON Dependency in Core**: Core 3D engine `ActDim.Three` uses `System.Text.Json` natively with typed primitive array buffers (`TypedArray`). Newtonsoft.Json adapters live in `ActDim.Three.NewtonsoftJson`.

## Solution Health & Verification
- **Solution Build & Pack**: All projects in `ActDim.Practix.sln` and `ActDim.Three.sln` build cleanly with 0 errors and 0 warnings.
- **Total Test Suite**: 560 tests passing in `ActDim.Practix.sln` plus 39 in `ActDim.Three.sln` with zero failures (599 / 599 total tests passing).
