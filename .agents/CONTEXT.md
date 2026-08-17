# Context

Current state snapshot of `actdim/practix` (.NET).

## Current Solution Architecture

### Autonomous Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`**: Roslyn-based C# evaluator (`ScriptEvaluator`) and template interpolation compiler (`InterpolationFormatter`).
- **`ActDim.Reflectron`**: High-performance reflection access (`TypeAccess`, `ObjectAccess`) and expression-tree property getters.
- **`ActDim.BlobManager`**: Keyed blob storage with sharded physical data store (`FileSystemBlobDataStore`), SQLite registry (`SQLiteBlobRegistry`), TTL expiration, and modular DI extension methods (`AddFileSystemBlobDataStore()`, `AddSQLiteBlobRegistry()`, `AddBlobManager()`).
- **`ActDim.Observability`**: OpenTelemetry-centric telemetry, ambient context management (`IObservabilityContext`, `EventObservabilityBridge`), and `AddEventObservability()` DI helper.

### Framework & Application Assemblies (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`**: Framework base interfaces and domain abstractions.
- **`ActDim.Practix.Common`**: Shared utilities, concurrent collection factories, compression, caching proxies, and granular Microsoft DI extensions (`AddAmbientContext()`, `AddCompressionManager()`, `AddMemoryCachingProxy()`, `AddDistributedCachingProxy()`).
- **`ActDim.Practix.Json`**: Dedicated JSON serialization assembly (`CoreJsonSerializer`, custom converters, policies, resolvers) leveraging `ActDim.Reflectron` for fast property setters and Microsoft DI extensions (`AddPractixJson()`, `AddJsonSerializer()`).
- **`ActDim.Practix.DataAccess`**: Data access layer and ORM integration.
- **`ActDim.Practix.Service`**: Primary backend service host using standard Microsoft DI (`IServiceCollection` / `AddCoreService()`).

## Dependency Injection Standard
- **Zero Autofac dependency**: Solution standardized completely on `Microsoft.Extensions.DependencyInjection` via granular extension methods (`AddAmbientContext`, `AddCompressionManager`, `AddMemoryCachingProxy`, `AddDistributedCachingProxy`, `AddPractixJson`, `AddCoreService`, `AddFileSystemBlobDataStore`, `AddSQLiteBlobRegistry`, `AddBlobManager`, `AddEventObservability`).

## Solution Health & Verification
- **Solution Build**: 14/14 projects building cleanly (`dotnet build ActDim.Practix.sln`).
- **Total Test Suite**: 484 tests passing across 6 test assemblies:
  - `ActDim.Practix.Json.Tests` (101 tests)
  - `ActDim.Practix.Common.Tests` (213 tests)
  - `ActDim.Emitron.Tests` (32 tests)
  - `ActDim.Reflectron.Tests` (42 tests)
  - `ActDim.BlobManager.Tests` (68 tests)
  - `ActDim.Observability.Tests` (28 tests)
- Zero warnings, zero failures.
