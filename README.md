# ActDim (.NET)

Modern, high-performance .NET libraries and application framework by Active Dimension.

## Solution Architecture

### Standalone Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`** — Dynamic Roslyn script execution engine (`ScriptEngine`) and C# interpolation template compiler (`Interpolator`, `template.Interpolate(input)`).
- **`ActDim.Reflectron`** — High-performance reflection engine, dynamic delegate compilation, and type/object member accessors (`TypeAccess`, `ObjectAccess`).
- **`ActDim.BytePath`** — Concurrency-aware blob and binary payload storage engine with decoupled registries, sharded filesystem data stores, TTL expiration, and distributed locks (`IBlobManager`, `AddBlobManager`).
- **`ActDim.BytePath.FileSystemStore`** — File-system blob data store implementation with sharded hash directories.
- **`ActDim.BytePath.SqliteRegistry`** — SQLite-backed ACID metadata registry with distributed locking and TTL.
- **`ActDim.Observability`** — OpenTelemetry-centric logging, activity tracing, and ambient context enrichment (`IObservabilityContext`, `AddEventObservability`).
- **`ActDim.Three`** — 3D scene graph, math, geometry, and serialization for Three.js compatibility.

### Framework & Application Modules (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`** — Core domain abstractions, design patterns, and framework contracts.
- **`ActDim.Practix.Common`** — Shared collection primitives, caching proxies, compression, and utilities.
- **`ActDim.Practix.Json`** — Autonomous high-performance JSON serialization subsystem backed by System.Text.Json.
- **`ActDim.Practix.DataAccess`** — Data access layer.
- **`ActDim.Practix.Service`** — Main application service host with standard Microsoft Dependency Injection.

## Testing & Verification

Run all unit tests across the solution:

```bash
dotnet test ActDim.Practix.sln
```
