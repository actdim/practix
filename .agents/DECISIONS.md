# Architecture Decisions (ADR)

## 2026-08-14 — ADR-001: Unified Observability Framework (`ActDim.Practix.Observability`)
- **Context:** The package was originally named `ActDim.Practix.Logging`, which reduced its conceptual value down to simple text logging.
- **Decision:** Renamed the package and namespace to `ActDim.Practix.Observability` and the core `ILogger` implementation to `EventObservabilityBridge`.
- **Consequences:** Clearly communicates that the package is a unified observability system connecting `Microsoft.Extensions.Logging` (logs) with `System.Diagnostics.Activity` (spans/traces).

## 2026-08-14 — ADR-002: Dynamic Telemetry & Provider Suppression via `ICallContext`
- **Context:** Developers needed the ability to selectively suppress telemetry tags, CallContext ambient data, console outputs, or specific logger sinks for sensitive/isolated execution blocks without affecting global options.
- **Decision:** Implemented dynamic suppression keys in `CallContextPropertyNames` (`IncludeExternalScopes`, `IncludeCallContext`, `SuppressConsole`, `SuppressedProviders`) and RAII extension methods on `ICallContext` (`SuppressExternalScopes()`, `SuppressCallContext()`, `SuppressConsole()`, `SuppressProviders()`).
- **Consequences:** Allows fine-grained per-async-scope suppression while preserving OpenTelemetry `Activity` trace collection.

## 2026-08-14 — ADR-003: Provider Alias Resolution using Official .NET `[ProviderAlias]` Attribute
- **Context:** Selective provider suppression (`SuppressProviders("Console")`) needed a clean way to match user provider names to concrete C# provider classes without manual string guessing.
- **Decision:** Use reflection to read the official Microsoft `[ProviderAlias]` attribute on registered `ILoggerProvider` types, falling back to class name matching or custom registrations in `EventObservabilityOptions`.
- **Consequences:** Ensures zero-configuration matching for standard .NET, Serilog, and NLog providers.

## 2026-08-14 — ADR-004: Ambient Status, Progress, Icon & Tag Telemetry Enrichment
- **Context:** Applications required a standard way to report real-time status text, progress percentage, emojis, and labels across traces and logs.
- **Decision:** Added `status`, `progress`, `icon`, and `tags` property keys to `CallContextPropertyNames` and extension methods `SetStatus()`, `ReportProgress()`, and `PushTags()`.
- **Consequences:** Automatically enriches OpenTelemetry `Activity` spans and events with progress and status metadata.
