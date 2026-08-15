# Context

Current state snapshot of `actdim/practix` (.NET).

## Current State
- Telemetry & Observability package: **`ActDim.Practix.Observability`**.
- Core bridge implementation: **`EventObservabilityBridge`** (`ILogger` & `ISupportExternalScope` decorator).
- DI Registration: **`services.AddEventObservability()`**.
- Signal separation (ADR-008) — the rule the whole bridge now follows:
  - **`Log`** produces a **log record only**. It writes no span attributes and no `ActivityEvent`; trace correlation of the record is filled by the logging pipeline (`LogRecord.TraceId` / `SpanId`). The single carve-out is a logged `Exception`, reported via `Activity.AddException` and deliberately independent of log level (`RecordExceptionsOnSpan`), and recorded at most once per span by `SpanExceptionRecorder` (ADR-010).
  - **`BeginScope`** owns the **span**: starts an `Activity` when none is current (ADR-007), then writes the exported telemetry properties, the scope state and — if enabled — external scopes as span attributes, independently of log level filtering.
  - **Span names** are low-cardinality (ADR-009): `LogEvent.Name` → raw `{OriginalFormat}` → state string → type name → `"Scope"`.
  - **Collisions** — all span writes go through `TelemetryTagCollector`; `TagCollisions` selects `KeepFirst` (default) / `Overwrite` / `Throw`, and any collision is exported as `log.collisions`. `EventObservabilityHelper.FlattenPairs` keeps DTO-level duplicates visible.
- Ambient telemetry state: **`IObservabilityContext`** (ADR-011, ADR-012), resolved from DI. `IAmbientContext` is its backing store and stays a neutral ambient variable bag with no telemetry meaning.
  - **Export is opt-in:** only properties set through `IObservabilityContext` are marked in `ExportedKeys` and become span attributes. Anything pushed straight into `IAmbientContext` never reaches telemetry.
  - **Data properties:** `SetStatus("Downloading", icon: "🚀")`, `SetProgress(45.5)`, `Push(name, value)` — written to `Activity.Current` at push time and restored on dispose; also replayed by the `BeginScope` pass when set before a span existed.
  - **Control switches** (never exported): `SuppressConsole()`, `SuppressProviders("File", "Console")`, `SuppressExternalScopes()`, `PushActivitySourceName("Custom.Worker")`. External scopes are off by default (`IncludeExternalScopes = false`).
  - **Provider Alias Resolution:** via official .NET `[ProviderAlias]` attributes; sources cached in `ActivitySourceRegistry`.
- Architectural Decision Records: `ADR-001` through `ADR-012` in `.agents/DECISIONS.md`.
- `Tests/Observability.Tests`: 24 tests passing; full solution builds.

## Known Gaps (see ISSUES board)
- A log call made without any scope produces no trace data (documented model, not a bug).
- Text sinks carry no TraceId/SpanId (`ActivityTrackingOptions` unset); the OTel logs pipeline is unaffected.
- Message-template analyzer not started; narrowed to `BeginScope` state after ADR-008. `.editorconfig` escalation of CA2017/CA2253/CA2254 is its step 0.
