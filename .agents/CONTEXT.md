# Context

Current state snapshot of `actdim/practix` (.NET).

## Current State
- Telemetry & Observability package: **`ActDim.Practix.Observability`**.
- Core bridge implementation: **`EventObservabilityBridge`** (`ILogger` & `ISupportExternalScope` decorator).
- DI Registration: **`services.AddEventObservability()`**.
- Ambient context features:
  - **Status & Progress:** `SetStatus("Downloading", icon: "🚀")`, `ReportProgress(45.5)`, `PushTags("billing")`.
  - **Selective Provider Suppression:** `SuppressConsole()`, `SuppressProviders("File", "Console")`, `SuppressExternalScopes()`, `SuppressCallContext()`.
  - **Provider Alias Resolution:** Automatic resolution via official .NET `[ProviderAlias]` attributes.
- Project Rules in `AGENTS.md`:
  - `DRY & Code Reusability`
  - `XML Documentation & Inheritdoc`
  - `Prefer Extension Methods`
  - `Production-Realistic Tests`
- Architectural Decision Records: `ADR-001` through `ADR-004` in `.agents/DECISIONS.md`.
- All 8 unit tests in `Tests/Observability.Tests` verified passing (`Passed: 8, Failed: 0`).
