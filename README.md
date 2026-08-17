# ActDim (.NET)

Modern, high-performance .NET libraries and application framework by Active Dimension.

## Solution Architecture

### Standalone Engine Libraries (`ActDim.*`)
- **`ActDim.Emitron`** — Dynamic Roslyn script evaluator (`ScriptEvaluator`) and C# interpolation template formatter (`InterpolationFormatter`).
- **`ActDim.Reflectron`** — Fast reflection, dynamic delegate compilation, and type/object member accessors (`TypeAccess`, `ObjectAccess`).
- **`ActDim.BlobManager`** — Concurrency-aware blob storage with metadata registry, TTL expiration, and distributed read/write locks (`IBlobManager`).
- **`ActDim.Observability`** — OpenTelemetry-centric logging, activity tracing, and ambient context enrichment (`IObservabilityContext`, `AddEventObservability`).

### Framework & Application Modules (`ActDim.Practix.*`)
- **`ActDim.Practix.Abstractions`** — Core domain abstractions and framework contracts.
- **`ActDim.Practix.Common`** — Shared collection primitives, extension methods, and helpers.
- **`ActDim.Practix.DataAccess`** — Data access layer.
- **`ActDim.Practix.Service`** — Main application service.

## Testing & Verification

Run all unit tests across the solution:

```bash
dotnet test ActDim.Practix.sln
```
