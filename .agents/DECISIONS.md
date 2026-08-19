# Architecture Decisions (ADR)

## 2026-08-14 — ADR-001: Unified Observability Framework (`ActDim.Observability`)
- **Context:** The package was originally named `ActDim.Observability`, which reduced its conceptual value down to simple text logging.
- **Decision:** Renamed the package and namespace to `ActDim.Observability` and the core `ILogger` implementation to `EventObservabilityBridge`.
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

## 2026-08-14 — ADR-005: Telemetry Tag Ownership — Span/Event Split and Reserved `log.*` Namespace
- **Context:** All four telemetry sources (bridge intrinsics, ambient `ICallContext`, external scopes, message-template placeholders) were written into one flat dictionary attached to every `ActivityEvent`, placeholders last. A placeholder named `{Message}`, `{Status}` or `{EventId}` silently overwrote the formatted message, the ambient status and the event id — verified in the real DI path. Ambient data was also copied into every event of a span.
- **Decision:**
  - Ambient `ICallContext` data and external scopes describe the operation and become **span** attributes (`Activity.SetTag`); the log call becomes an `ActivityEvent` carrying only the template placeholders and bridge-owned tags.
  - Bridge-owned tags move into a reserved namespace: `log.message`, `log.level`, `log.event.id`, `log.collisions` (see `ObservabilityTagNames`). Dotted form is used because `ToOtelName` emits dotted names exclusively; a snake_case segment would be a second naming dialect.
  - `log.level` is recorded — previously the log level was absent from telemetry entirely.
  - Tag writes go through `TelemetryTagCollector`, which applies `TagCollisionBehavior` (`KeepFirst` by default, `Overwrite`, `Throw` for tests) and always counts collisions. A non-zero count is exported as `log.collisions`, so remaining collisions — DTO flattening, `LogEvent.ActivityTags`, ambient vs scope — never stay invisible.
- **Consequences:** Breaking change for consumers querying `message` / `event.id` / ambient tags on events. No compatibility flag was introduced: the clean layout was chosen over carrying an unused legacy branch. Ambient data is written once per span instead of once per log call, reducing exported payload. Remaining statically detectable collisions are deferred to a future Roslyn analyzer.
- **Status:** Event-level part superseded by ADR-008 — log calls no longer emit an `ActivityEvent`, so `log.message` / `log.level` / `log.event.id` are gone. The span/event ownership rule and the collision policy remain in force.

## 2026-08-14 — ADR-006: Exceptions Recorded via `Activity.AddException`
- **Context:** The bridge wrote `exception.type` / `exception.message` / `exception.stacktrace` manually as tags on the log event, occupying three reserved names in the shared dictionary and diverging from the OpenTelemetry convention.
- **Decision:** Use `Activity.AddException(exception)` (available on .NET 9+; the project targets net10.0), which emits a dedicated `exception` event with the standard attributes.
- **Consequences:** Traces are consumable by any OTLP backend without custom mapping, and `exception.*` disappears from the log event's tag space. The exception and the log line become two ordered events instead of one.

## 2026-08-14 — ADR-007: Automatic Activity Span Creation on BeginScope with CallContext Source Override
- **Context:** Operations and log calls executed outside an active distributed trace span (`Activity.Current == null`) produced no OpenTelemetry span data, causing background workers and startup/shutdown flows to be invisible in trace backends.
- **Decision:**
  - `EventObservabilityBridge.BeginScope` automatically starts an `Activity` span when `Activity.Current` is null and `options.AutoCreateActivityOnScope` is enabled (default `true`).
  - Source resolution follows a priority chain: `callContext.PushActivitySourceName(...)` > `EventObservabilityOptions.DefaultActivitySourceName` (`Assembly.GetEntryAssembly()?.GetName().Name ?? "ActDim.Practix"`).
  - All `ActivitySource` instances are managed as long-lived singletons in `ActivitySourceRegistry` to prevent allocation and runtime listener leak.
  - The created `Activity` lifecycle is bound to the `IDisposable` returned from `BeginScope`.
- **Consequences:** Ensures seamless trace coverage for scoped blocks while allowing callers to assign specific source names without manual `ActivitySource` boilerplate.

## 2026-08-15 — ADR-008: Signal Separation — `Log` Produces a Log Record, `BeginScope` Owns the Span
- **Context:** ADR-005 split span and event levels, but the log call was still the code path that wrote ambient context and external scopes onto the span and emitted an `ActivityEvent` per log line. Because that path sits behind `_inner.IsEnabled(logLevel)`, raising the minimum log level silently stripped the operation context from traces: measured with `SetMinimumLevel(Warning)`, a span whose scope logged only `Information` lost `tenant.id` and `status` entirely. Duplicating every log line as a span event also competed with the OTel logs pipeline, which already carries the same data with native trace correlation.
- **Decision:**
  - `Log` writes a log record only. No span attributes, no `ActivityEvent`. Trace correlation of that record is the logging pipeline's job (`LogRecord.TraceId` / `SpanId` are filled from `Activity.Current` natively).
  - `BeginScope` owns the trace side: it starts the span when needed and writes the ambient `ICallContext`, the external scopes and the scope state as span attributes — independently of log level filtering.
  - The single exception is failure reporting: a logged `Exception` still reaches the current span through `Activity.AddException`, deliberately ungated by `IsEnabled`, controlled by `EventObservabilityOptions.RecordExceptionsOnSpan` (default `true`).
  - External scopes are collected only for a span started by that scope, since scopes opened earlier had no span to write to.
- **Consequences:** Supersedes the event-level part of ADR-005: `log.message`, `log.level` and `log.event.id` are no longer emitted and were removed from `ObservabilityTagNames`; only `log.collisions` remains. ADR-006 stays, narrowed to the exception carve-out. Trace content no longer depends on sink configuration, closing `bug--span-enrichment-gated-by-log-level`. Two known consequences remain open: ambient properties pushed *after* the scope was opened never reach the span, and a log call made without any scope produces no trace data at all.

## 2026-08-15 — ADR-009: Low-Cardinality Span Names for Auto-Created Activities
- **Context:** The auto-created span took its name from `state.ToString()`, which yields the *formatted* message: `BeginScope("Processing order {OrderId}", 42)` produced a span named `Processing order 42`, a new name per order. A `LogEvent` scope produced the class FQN and a DTO scope produced `{ TenantId = acme, Attempt = 3 }`. Span names identify an operation and are the primary grouping key in trace backends, so unbounded cardinality breaks aggregation.
- **Decision:** Derive the name in this order: `LogEvent.Name` → the raw `{OriginalFormat}` template → the state string itself → the state type name for non-compiler-generated types → `"Scope"`. The formatted state is never used; per-call values reach the span as attributes instead.
- **Consequences:** `BeginScope("Processing order {OrderId}", 42)` and the same call for order 43 now share the span name `Processing order {OrderId}`. Anonymous-type scopes are named `Scope`, which is deliberately uninformative — identity for such scopes belongs in `LogEvent.Name` or a template.

## 2026-08-15 — ADR-010: An Exception Is Recorded at Most Once per Span
- **Context:** Because the exception carve-out of ADR-008 records at the point of *logging*, the ordinary catch / log / rethrow pattern reports the same exception instance on every layer it passes. Measured across three layers of one operation: three identical `exception` events on one span, same type, same message, same stack trace — and the stack trace is the heaviest attribute the bridge emits.
- **Decision:** `SpanExceptionRecorder` tracks, per exception instance, the set of spans it has already been recorded on, using a `ConditionalWeakTable` keyed by the exception. A repeated report on the same span is skipped; a report on a different span still records, so an exception propagating into an enclosing operation marks that operation too.
- **Consequences (ADR-010):** The key is held weakly, so an entry lives exactly as long as the exception — no leak and no mutation of user objects. Identity survives `throw;` and `await` (`ExceptionDispatchInfo` rethrows the same instance), both verified; wrapping into a new exception produces a separate record, which is the intended behaviour. The span set is guarded by a lock, since the same instance can be reported concurrently from several threads.

## 2026-08-15 — ADR-011: `IObservabilityContext` — Telemetry Ambient State Separated from `ICallContext`
- **Context:** ADR-008 made `BeginScope` snapshot the ambient context into span attributes, which left properties set *after* the scope was opened unexported — and `SetStatus` / `ReportProgress` are meant to be called as an operation progresses, so that is the common ordering. Exporting them at push time makes the store a telemetry concept, and the store was `ICallContext` — a neutral ambient variable bag in `Abstractions`. The boundary was already broken in practice: `CallContextPropertyNames` consisted *entirely* of telemetry keys (`status`, `progress`, `icon`, `tags`, `__Practix_SuppressConsole`, `__Practix_ActivitySourceName`), so the neutral abstraction already knew about console providers and `ActivitySource`.
- **Decision:**
  - Introduce `IObservabilityContext` in `ActDim.Observability`, registered by `AddEventObservability`, as the owner of telemetry ambient state. `ICallContext` returns to being a neutral store and remains its backing storage, so ambient values stay readable by non-telemetry consumers such as the planned console spinner.
  - Data properties (`status`, `progress`, `icon`, `tags`, arbitrary `Push`) are written to `Activity.Current` at push time and restored on dispose — the previous attribute value is captured and put back, and a previously absent one is removed. The span active at push time is captured deliberately, since `Activity.Current` may differ on dispose.
  - The `BeginScope` snapshot stays: it covers properties set *before* a span exists. The two mechanisms are complementary and both are required.
  - Control switches (`__Practix_*`, `ActivitySourceName`) are never exported. `SuppressAmbientProperties` suppresses both the immediate export and the snapshot, but cannot retract an attribute already sent.
  - `CallContextPropertyNames` moved to `ObservabilityContextPropertyNames`; the telemetry extension methods left `ActDim.Practix.Common`; the telemetry entries left the `CallContextProperty` enum in `Abstractions`.
- **Consequences:** Breaking API change — `callContext.SetStatus(...)` becomes `observability.SetStatus(...)`, resolved from DI; done in one step since the package has no consumers yet. `ActDim.Practix.Common` and `ActDim.Practix.Abstractions` no longer know about telemetry. `SuppressCallContext` was renamed to `SuppressAmbientProperties` to say what it actually suppresses.

## 2026-08-15 — ADR-012: `IAmbientContext` Renaming & Explicit `Activity` Telemetry Export Policy
- **Context:** Previously, `ICallContext` (an old .NET Remoting name) was read greedily by `EventObservabilityBridge.EnrichSpanFromScope`, dumping all ambient variables and all external scopes (`IExternalScopeProvider`, e.g. ASP.NET Core `ActionDescriptor`, routing data) into `Activity` tags (OpenTelemetry span attributes). This caused heavy internal business objects and framework internals to pollute distributed trace spans and exceed OpenTelemetry span attribute limits.
- **Decision:**
  - Rename `ICallContext` / `ICallContextProvider` / `CallContext` to `IAmbientContext` / `IAmbientContextProvider` / `AmbientContext` (and `Data` to `Properties`, `Push` to `PushProperty`). `IAmbientContext` is a purely neutral ambient variable bag and **never** automatically exports into `Activity` tags.
  - Telemetry state is owned exclusively by `IObservabilityContext`. Properties pushed via `IObservabilityContext` (`SetStatus`, `SetProgress`, `Push`) are explicitly tracked in `__Practix_ExportedKeys` in ambient state, written to `Activity.Current` immediately if active, and snapshotted to newly created `Activity` spans in `BeginScope`.
  - `IncludeExternalScopes` default is changed from `true` to `false` (external scopes belong to log formatters, not `Activity` spans), but can be enabled in options or suppressed via `SuppressExternalScopes()`.
  - Removed obsolete `PushTags(params string[])`, `IncludeCallContext`, and `SuppressAmbientProperties()`.
  - Documentation and XML comments consistently use .NET BCL terminology (`Activity`, `Activity.SetTag`).
- **Consequences:** Eliminates greedy span pollution and payload bloat. Developers can safely store heavy objects in `IAmbientContext` without trace side effects. Clean separation between ambient application state and telemetry state.
## 2026-08-17 — ADR-013: Domain Exception Hierarchy — Introduce `DataFormatException` & Relocate `IncompleteDataException` to `Abstractions`
- **Context:** `IncompleteDataException` and custom `InvalidDataException` were declared in `ActDim.Practix.Common`. Custom `InvalidDataException` collided directly with .NET BCL's `System.IO.InvalidDataException`, causing namespace shadowing. Furthermore, format validation (such as archive or compression header detection) and data payload parsing needed a dedicated, non-string-bound domain exception.
- **Decision:**
  - Introduced `DataFormatException` in `ActDim.Practix.Abstractions/Exceptions/DataFormatException.cs` (namespace `ActDim.Practix.Abstractions.Exceptions`) for data structure, payload, and protocol format errors.
  - Relocated `IncompleteDataException` to `ActDim.Practix.Abstractions/Exceptions/IncompleteDataException.cs` under `ActDim.Practix.Abstractions.Exceptions` and derived it from `DataFormatException` (`IncompleteDataException : DataFormatException`).
  - Deleted custom `ActDim.Practix.Common.InvalidDataException` to eliminate shadowing with BCL `System.IO.InvalidDataException`.
- **Consequences:** Provides a clean, expressively typed domain exception hierarchy in `Abstractions` (`DataFormatException` -> `IncompleteDataException`) with zero BCL naming collisions.

## 2026-08-19 — ADR-014: Extension Method Organization, Target Namespace Alignment, and DI Standardization
- **Context:** Extension methods across projects were stored in arbitrary non-standard folders and namespaces. DI registration methods on `IServiceCollection` were placed in ad-hoc `.Extensions` namespaces requiring manual using directives in Startup/Program.cs. Third-party and domain extensions (e.g. `GuardExtensions`, `MemoryCacheExtensions`, `MemoryStreamManagerExtensions`, `SceneDocumentExtensions`) were placed in generic namespace bags rather than matching the target types they extend. Additionally, `EnumerableExtensions.Partition` and `FuncExtensions.Memoize` contained legacy experimental code and blocking concurrency primitives.
- **Decision:**
  - Standardize all extension classes into `Extensions/` subfolders across all projects.
  - Set all DI `IServiceCollection` extension methods across all projects to `namespace Microsoft.Extensions.DependencyInjection` per Microsoft Framework Design Guidelines.
  - Align non-system domain and third-party extensions with the namespaces of the target types they extend (`Ardalis.GuardClauses`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.IO`, `ActDim.Three.Core`, `ActDim.Three.Core.Buffers`).
  - Keep general BCL utility extensions in modular namespaces (`ActDim.Practix.Extensions`, `ActDim.Reflectron`, `ActDim.Emitron`) to avoid global `System.*` IntelliSense clutter.
  - Refactor `EnumerableExtensions.Partition` to use runtime-optimized `source.Chunk(size)`, optimize `MinOrDefault`/`MaxOrDefault` to single-pass, and refactor `FuncExtensions.Memoize` to lock-free `ConcurrentFactoryDictionary`.
- **Consequences:** Clean, idiomatic .NET API ergonomics, automatic DI method discovery without cluttering using lists, zero-lock concurrency for function memoization, and elimination of legacy dead code.
