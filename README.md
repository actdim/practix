# ActDim (.NET)

Modern, high-performance .NET libraries and application framework by Active Dimension.

## Solution Architecture

### Standalone Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`** — Dynamic Roslyn script execution engine and C# interpolation template compiler (`Emitron`, `Interpolator`, `template.Interpolate(input)`).
- **`ActDim.Reflectron`** — High-performance reflection engine, expression-tree delegate compilation, and fluent weak-referenced member accessors (`Reflectron`, `IReflectron<T>`, `obj.Reflect()`).
- **`ActDim.BytePath`** — Concurrency-aware blob and binary payload storage engine with decoupled registries, sharded filesystem data stores, TTL expiration, and distributed locks (`IBlobManager`, `AddBlobManager`).
- **`ActDim.BytePath.FileSystemStore`** — File-system blob data store implementation with sharded hash directories.
- **`ActDim.BytePath.SqliteRegistry`** — SQLite-backed ACID metadata registry with distributed locking and TTL.
- **`ActDim.Observability`** — OpenTelemetry-centric logging, activity tracing, and ambient context enrichment (`IObservabilityContext`, `AddEventObservability`).
- **`ActDim.Three`** — 3D scene graph, math, geometry, and serialization for Three.js compatibility (`ThreeSerializer`, `SceneDocument`).

### Framework & Application Modules (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`** — Core domain abstractions, design patterns, and framework contracts.
- **`ActDim.Practix.Common`** — Shared collection primitives, caching proxies, compression, and utilities.
- **`ActDim.Practix.Json`** — Autonomous high-performance JSON serialization subsystem backed by System.Text.Json.
- **`ActDim.Practix.DataAccess`** — Data access layer.
- **`ActDim.Practix.Service`** — Main application service host with standard Microsoft Dependency Injection.

---

## Test Verification & Quality Status

All test suites are passing with a **100% success rate (586 / 586 tests passing, 0 failed, 0 skipped)**:

| Test Project | Target Library / Module | Tests Passed | Failed | Skipped | Status |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **`ActDim.Emitron.Tests`** | `ActDim.Emitron` | **54** | 0 | 0 | ✅ Passed |
| **`ActDim.Reflectron.Tests`** | `ActDim.Reflectron` | **56** | 0 | 0 | ✅ Passed |
| **`ActDim.Practix.Common.Tests`** | `ActDim.Practix.Common` | **235** | 0 | 0 | ✅ Passed |
| **`ActDim.Practix.Json.Tests`** | `ActDim.Practix.Json` | **102** | 0 | 0 | ✅ Passed |
| **`ActDim.BytePath.Tests`** | `ActDim.BytePath` | **74** | 0 | 0 | ✅ Passed |
| **`ActDim.Three.Tests`** | `ActDim.Three` | **35** | 0 | 0 | ✅ Passed |
| **`ActDim.Observability.Tests`** | `ActDim.Observability` | **30** | 0 | 0 | ✅ Passed |
| **TOTAL** | **Entire Solution** | **586** | **0** | **0** | **100% Passing** |

### Run Tests

Run all tests across solutions:

```bash
# Core framework & engines (551 tests)
dotnet test ActDim.Practix.sln

# 3D Math & Scene Graph (35 tests)
dotnet test ActDim.Three.sln
```
