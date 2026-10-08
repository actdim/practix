# Architecture Decisions (ADR)

## 2026-08-14: ADR-001: Unified Observability Framework (`ActDim.Observability`)
- **Context:** The package was originally named `ActDim.Observability`, which reduced its conceptual value down to simple text logging.
- **Decision:** Renamed the package and namespace to `ActDim.Observability` and the core `ILogger` implementation to `EventObservabilityBridge`.
- **Consequences:** Clearly communicates that the package is a unified observability system connecting `Microsoft.Extensions.Logging` (logs) with `System.Diagnostics.Activity` (spans/traces).

## 2026-08-14: ADR-002: Dynamic Telemetry & Provider Suppression via `ICallContext`
- **Context:** Developers needed the ability to selectively suppress telemetry tags, CallContext ambient data, console outputs, or specific logger sinks for sensitive/isolated execution blocks without affecting global options.
- **Decision:** Implemented dynamic suppression keys in `CallContextPropertyNames` (`IncludeExternalScopes`, `IncludeCallContext`, `SuppressConsole`, `SuppressedProviders`) and RAII extension methods on `ICallContext` (`SuppressExternalScopes()`, `SuppressCallContext()`, `SuppressConsole()`, `SuppressProviders()`).
- **Consequences:** Allows fine-grained per-async-scope suppression while preserving OpenTelemetry `Activity` trace collection.

## 2026-08-14: ADR-003: Provider Alias Resolution using Official .NET `[ProviderAlias]` Attribute
- **Context:** Selective provider suppression (`SuppressProviders("Console")`) needed a clean way to match user provider names to concrete C# provider classes without manual string guessing.
- **Decision:** Use reflection to read the official Microsoft `[ProviderAlias]` attribute on registered `ILoggerProvider` types, falling back to class name matching or custom registrations in `EventObservabilityOptions`.
- **Consequences:** Ensures zero-configuration matching for standard .NET, Serilog, and NLog providers.

## 2026-08-14: ADR-004: Ambient Status, Progress, Icon & Tag Telemetry Enrichment
- **Context:** Applications required a standard way to report real-time status text, progress percentage, emojis, and labels across traces and logs.
- **Decision:** Added `status`, `progress`, `icon`, and `tags` property keys to `CallContextPropertyNames` and extension methods `SetStatus()`, `ReportProgress()`, and `PushTags()`.
- **Consequences:** Automatically enriches OpenTelemetry `Activity` spans and events with progress and status metadata.

## 2026-08-14: ADR-005: Telemetry Tag Ownership: Span/Event Split and Reserved `log.*` Namespace
- **Context:** All four telemetry sources (bridge intrinsics, ambient `ICallContext`, external scopes, message-template placeholders) were written into one flat dictionary attached to every `ActivityEvent`, placeholders last. A placeholder named `{Message}`, `{Status}` or `{EventId}` silently overwrote the formatted message, the ambient status and the event id: verified in the real DI path. Ambient data was also copied into every event of a span.
- **Decision:**
  - Ambient `ICallContext` data and external scopes describe the operation and become **span** attributes (`Activity.SetTag`); the log call becomes an `ActivityEvent` carrying only the template placeholders and bridge-owned tags.
  - Bridge-owned tags move into a reserved namespace: `log.message`, `log.level`, `log.event.id`, `log.collisions` (see `ObservabilityTagNames`). Dotted form is used because `ToOtelName` emits dotted names exclusively; a snake_case segment would be a second naming dialect.
  - `log.level` is recorded: previously the log level was absent from telemetry entirely.
  - Tag writes go through `TelemetryTagCollector`, which applies `TagCollisionBehavior` (`KeepFirst` by default, `Overwrite`, `Throw` for tests) and always counts collisions. A non-zero count is exported as `log.collisions`, so remaining collisions: DTO flattening, `LogEvent.ActivityTags`, ambient vs scope: never stay invisible.
- **Consequences:** Breaking change for consumers querying `message` / `event.id` / ambient tags on events. No compatibility flag was introduced: the clean layout was chosen over carrying an unused legacy branch. Ambient data is written once per span instead of once per log call, reducing exported payload. Remaining statically detectable collisions are deferred to a future Roslyn analyzer.
- **Status:** Event-level part superseded by ADR-008: log calls no longer emit an `ActivityEvent`, so `log.message` / `log.level` / `log.event.id` are gone. The span/event ownership rule and the collision policy remain in force.

## 2026-08-14: ADR-006: Exceptions Recorded via `Activity.AddException`
- **Context:** The bridge wrote `exception.type` / `exception.message` / `exception.stacktrace` manually as tags on the log event, occupying three reserved names in the shared dictionary and diverging from the OpenTelemetry convention.
- **Decision:** Use `Activity.AddException(exception)` (available on .NET 9+; the project targets net10.0), which emits a dedicated `exception` event with the standard attributes.
- **Consequences:** Traces are consumable by any OTLP backend without custom mapping, and `exception.*` disappears from the log event's tag space. The exception and the log line become two ordered events instead of one.

## 2026-08-14: ADR-007: Automatic Activity Span Creation on BeginScope with CallContext Source Override
- **Context:** Operations and log calls executed outside an active distributed trace span (`Activity.Current == null`) produced no OpenTelemetry span data, causing background workers and startup/shutdown flows to be invisible in trace backends.
- **Decision:**
  - `EventObservabilityBridge.BeginScope` automatically starts an `Activity` span when `Activity.Current` is null and `options.AutoCreateActivityOnScope` is enabled (default `true`).
  - Source resolution follows a priority chain: `callContext.PushActivitySourceName(...)` > `EventObservabilityOptions.DefaultActivitySourceName` (`Assembly.GetEntryAssembly()?.GetName().Name ?? "ActDim.Practix"`).
  - All `ActivitySource` instances are managed as long-lived singletons in `ActivitySourceRegistry` to prevent allocation and runtime listener leak.
  - The created `Activity` lifecycle is bound to the `IDisposable` returned from `BeginScope`.
- **Consequences:** Ensures seamless trace coverage for scoped blocks while allowing callers to assign specific source names without manual `ActivitySource` boilerplate.

## 2026-08-15: ADR-008: Signal Separation: `Log` Produces a Log Record, `BeginScope` Owns the Span
- **Context:** ADR-005 split span and event levels, but the log call was still the code path that wrote ambient context and external scopes onto the span and emitted an `ActivityEvent` per log line. Because that path sits behind `_inner.IsEnabled(logLevel)`, raising the minimum log level silently stripped the operation context from traces: measured with `SetMinimumLevel(Warning)`, a span whose scope logged only `Information` lost `tenant.id` and `status` entirely. Duplicating every log line as a span event also competed with the OTel logs pipeline, which already carries the same data with native trace correlation.
- **Decision:**
  - `Log` writes a log record only. No span attributes, no `ActivityEvent`. Trace correlation of that record is the logging pipeline's job (`LogRecord.TraceId` / `SpanId` are filled from `Activity.Current` natively).
  - `BeginScope` owns the trace side: it starts the span when needed and writes the ambient `ICallContext`, the external scopes and the scope state as span attributes: independently of log level filtering.
  - The single exception is failure reporting: a logged `Exception` still reaches the current span through `Activity.AddException`, deliberately ungated by `IsEnabled`, controlled by `EventObservabilityOptions.RecordExceptionsOnSpan` (default `true`).
  - External scopes are collected only for a span started by that scope, since scopes opened earlier had no span to write to.
- **Consequences:** Supersedes the event-level part of ADR-005: `log.message`, `log.level` and `log.event.id` are no longer emitted and were removed from `ObservabilityTagNames`; only `log.collisions` remains. ADR-006 stays, narrowed to the exception carve-out. Trace content no longer depends on sink configuration, closing `bug--span-enrichment-gated-by-log-level`. Two known consequences remain open: ambient properties pushed *after* the scope was opened never reach the span, and a log call made without any scope produces no trace data at all.

## 2026-08-15: ADR-009: Low-Cardinality Span Names for Auto-Created Activities
- **Context:** The auto-created span took its name from `state.ToString()`, which yields the *formatted* message: `BeginScope("Processing order {OrderId}", 42)` produced a span named `Processing order 42`, a new name per order. A `LogEvent` scope produced the class FQN and a DTO scope produced `{ TenantId = acme, Attempt = 3 }`. Span names identify an operation and are the primary grouping key in trace backends, so unbounded cardinality breaks aggregation.
- **Decision:** Derive the name in this order: `LogEvent.Name` → the raw `{OriginalFormat}` template → the state string itself → the state type name for non-compiler-generated types → `"Scope"`. The formatted state is never used; per-call values reach the span as attributes instead.
- **Consequences:** `BeginScope("Processing order {OrderId}", 42)` and the same call for order 43 now share the span name `Processing order {OrderId}`. Anonymous-type scopes are named `Scope`, which is deliberately uninformative: identity for such scopes belongs in `LogEvent.Name` or a template.

## 2026-08-15: ADR-010: An Exception Is Recorded at Most Once per Span
- **Context:** Because the exception carve-out of ADR-008 records at the point of *logging*, the ordinary catch / log / rethrow pattern reports the same exception instance on every layer it passes. Measured across three layers of one operation: three identical `exception` events on one span, same type, same message, same stack trace: and the stack trace is the heaviest attribute the bridge emits.
- **Decision:** `SpanExceptionRecorder` tracks, per exception instance, the set of spans it has already been recorded on, using a `ConditionalWeakTable` keyed by the exception. A repeated report on the same span is skipped; a report on a different span still records, so an exception propagating into an enclosing operation marks that operation too.
- **Consequences (ADR-010):** The key is held weakly, so an entry lives exactly as long as the exception: no leak and no mutation of user objects. Identity survives `throw;` and `await` (`ExceptionDispatchInfo` rethrows the same instance), both verified; wrapping into a new exception produces a separate record, which is the intended behaviour. The span set is guarded by a lock, since the same instance can be reported concurrently from several threads.

## 2026-08-15: ADR-011: `IObservabilityContext`: Telemetry Ambient State Separated from `ICallContext`
- **Context:** ADR-008 made `BeginScope` snapshot the ambient context into span attributes, which left properties set *after* the scope was opened unexported: and `SetStatus` / `ReportProgress` are meant to be called as an operation progresses, so that is the common ordering. Exporting them at push time makes the store a telemetry concept, and the store was `ICallContext`: a neutral ambient variable bag in `Abstractions`. The boundary was already broken in practice: `CallContextPropertyNames` consisted *entirely* of telemetry keys (`status`, `progress`, `icon`, `tags`, `__Practix_SuppressConsole`, `__Practix_ActivitySourceName`), so the neutral abstraction already knew about console providers and `ActivitySource`.
- **Decision:**
  - Introduce `IObservabilityContext` in `ActDim.Observability`, registered by `AddEventObservability`, as the owner of telemetry ambient state. `ICallContext` returns to being a neutral store and remains its backing storage, so ambient values stay readable by non-telemetry consumers such as the planned console spinner.
  - Data properties (`status`, `progress`, `icon`, `tags`, arbitrary `Push`) are written to `Activity.Current` at push time and restored on dispose: the previous attribute value is captured and put back, and a previously absent one is removed. The span active at push time is captured deliberately, since `Activity.Current` may differ on dispose.
  - The `BeginScope` snapshot stays: it covers properties set *before* a span exists. The two mechanisms are complementary and both are required.
  - Control switches (`__Practix_*`, `ActivitySourceName`) are never exported. `SuppressAmbientProperties` suppresses both the immediate export and the snapshot, but cannot retract an attribute already sent.
  - `CallContextPropertyNames` moved to `ObservabilityContextPropertyNames`; the telemetry extension methods left `ActDim.Practix.Common`; the telemetry entries left the `CallContextProperty` enum in `Abstractions`.
- **Consequences:** Breaking API change: `callContext.SetStatus(...)` becomes `observability.SetStatus(...)`, resolved from DI; done in one step since the package has no consumers yet. `ActDim.Practix.Common` and `ActDim.Practix.Abstractions` no longer know about telemetry. `SuppressCallContext` was renamed to `SuppressAmbientProperties` to say what it actually suppresses.

## 2026-08-15: ADR-012: `IAmbientContext` Renaming & Explicit `Activity` Telemetry Export Policy
- **Context:** Previously, `ICallContext` (an old .NET Remoting name) was read greedily by `EventObservabilityBridge.EnrichSpanFromScope`, dumping all ambient variables and all external scopes (`IExternalScopeProvider`, e.g. ASP.NET Core `ActionDescriptor`, routing data) into `Activity` tags (OpenTelemetry span attributes). This caused heavy internal business objects and framework internals to pollute distributed trace spans and exceed OpenTelemetry span attribute limits.
- **Decision:**
  - Rename `ICallContext` / `ICallContextProvider` / `CallContext` to `IAmbientContext` / `IAmbientContextProvider` / `AmbientContext` (and `Data` to `Properties`, `Push` to `PushProperty`). `IAmbientContext` is a purely neutral ambient variable bag and **never** automatically exports into `Activity` tags.
  - Telemetry state is owned exclusively by `IObservabilityContext`. Properties pushed via `IObservabilityContext` (`SetStatus`, `SetProgress`, `Push`) are explicitly tracked in `__Practix_ExportedKeys` in ambient state, written to `Activity.Current` immediately if active, and snapshotted to newly created `Activity` spans in `BeginScope`.
  - `IncludeExternalScopes` default is changed from `true` to `false` (external scopes belong to log formatters, not `Activity` spans), but can be enabled in options or suppressed via `SuppressExternalScopes()`.
  - Removed obsolete `PushTags(params string[])`, `IncludeCallContext`, and `SuppressAmbientProperties()`.
  - Documentation and XML comments consistently use .NET BCL terminology (`Activity`, `Activity.SetTag`).
- **Consequences:** Eliminates greedy span pollution and payload bloat. Developers can safely store heavy objects in `IAmbientContext` without trace side effects. Clean separation between ambient application state and telemetry state.
## 2026-08-17: ADR-013: Domain Exception Hierarchy: Introduce `DataFormatException` & Relocate `IncompleteDataException` to `Abstractions`
- **Context:** `IncompleteDataException` and custom `InvalidDataException` were declared in `ActDim.Practix.Common`. Custom `InvalidDataException` collided directly with .NET BCL's `System.IO.InvalidDataException`, causing namespace shadowing. Furthermore, format validation (such as archive or compression header detection) and data payload parsing needed a dedicated, non-string-bound domain exception.
- **Decision:**
  - Introduced `DataFormatException` in `ActDim.Practix.Abstractions/Exceptions/DataFormatException.cs` (namespace `ActDim.Practix.Abstractions.Exceptions`) for data structure, payload, and protocol format errors.
  - Relocated `IncompleteDataException` to `ActDim.Practix.Abstractions/Exceptions/IncompleteDataException.cs` under `ActDim.Practix.Abstractions.Exceptions` and derived it from `DataFormatException` (`IncompleteDataException : DataFormatException`).
  - Deleted custom `ActDim.Practix.Common.InvalidDataException` to eliminate shadowing with BCL `System.IO.InvalidDataException`.
- **Consequences:** Provides a clean, expressively typed domain exception hierarchy in `Abstractions` (`DataFormatException` -> `IncompleteDataException`) with zero BCL naming collisions.

## 2026-08-19: ADR-014: Extension Method Organization, Target Namespace Alignment, and DI Standardization
- **Context:** Extension methods across projects were stored in arbitrary non-standard folders and namespaces. DI registration methods on `IServiceCollection` were placed in ad-hoc `.Extensions` namespaces requiring manual using directives in Startup/Program.cs. Third-party and domain extensions (e.g. `GuardExtensions`, `MemoryCacheExtensions`, `MemoryStreamManagerExtensions`, `SceneDocumentExtensions`) were placed in generic namespace bags rather than matching the target types they extend. Additionally, `EnumerableExtensions.Partition` and `FuncExtensions.Memoize` contained legacy experimental code and blocking concurrency primitives.
- **Decision:**
  - Standardize all extension classes into `Extensions/` subfolders across all projects.
  - Set all DI `IServiceCollection` extension methods across all projects to `namespace Microsoft.Extensions.DependencyInjection` per Microsoft Framework Design Guidelines.
  - Align non-system domain and third-party extensions with the namespaces of the target types they extend (`Ardalis.GuardClauses`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.IO`, `ActDim.Three.Core`, `ActDim.Three.Core.Buffers`).
  - Keep general BCL utility extensions in modular namespaces (`ActDim.Practix.Extensions`, `ActDim.Reflectron`, `ActDim.Emitron`) to avoid global `System.*` IntelliSense clutter.
  - Refactor `EnumerableExtensions.Partition` to use runtime-optimized `source.Chunk(size)`, optimize `MinOrDefault`/`MaxOrDefault` to single-pass, and refactor `FuncExtensions.Memoize` to lock-free `ConcurrentFactoryDictionary`.
- **Consequences:** Clean, idiomatic .NET API ergonomics, automatic DI method discovery without cluttering using lists, zero-lock concurrency for function memoization, and elimination of legacy dead code.

## 2026-08-19: ADR-015: Direct AsyncLocal Storage in `AmbientContext` and Elimination of `AmbientContextProvider`
- **Context:** Previously, `IAmbientContextProvider` and `AmbientContextProvider` were maintained as a separate DI provider layer by analogy with `IHttpContextAccessor`. However, `AmbientContext` itself is a stateless facade over `AsyncLocal<ImmutableDictionary<string, object>>`. Having both `IAmbientContextProvider` and `IAmbientContext` created redundant indirection, requiring consumers to call `provider.Get()` and confusing developers about which abstraction to inject into DI constructors.
- **Decision:**
  - Deleted `IAmbientContextProvider` and `AmbientContextProvider`.
  - Embedded `AsyncLocal<ImmutableDictionary<string, object>>` directly in `AmbientContext`.
  - Registered `IAmbientContext` directly as a singleton (`services.AddSingleton<IAmbientContext>(AmbientContext.Current)`).
  - Updated `ActDim.Observability` (`ObservabilityContext`, `EventObservabilityLoggerFactory`, `EventObservabilityBridge`, and `EventObservabilityExtensions`) to consume `IAmbientContext` directly.
- **Consequences:** Eliminates two redundant types, removes intermediate `.Get()` calls, streamlines DI constructor injection, and keeps runtime behavior 100% identical and isolated per async flow.

## 2026-08-19: ADR-016: Scoped Ambient Execution Primitives, Centralized `AmbientKeys`, and Solution-Wide Nullable Annotations
- **Context:** Applications required uniform access to ambient execution context state (`Services`, `User`, `CancellationToken`, `Blobs`, `LoggerFactory`) across console, background worker, and web application lifecycles. Global fallback mutable static state (`_defaultServices`) was rejected in favor of pure RAII scoping (`using (AmbientContext.WithServices(...))`). Furthermore, BytePath storage interfaces needed placement in `ActDim.Practix.Abstractions` for decoupled access, and nullable compiler diagnostics needed unified configuration across all 24 projects in the solution.
- **Decision:**
  - Placed core storage abstractions (`IBlobManager`, `IBlobDataStore`, `IBlobRegistry`, etc.) and `AmbientKeys` in `ActDim.Practix.Abstractions`.
  - Implemented typed extension methods on `IAmbientContext` (`AmbientContextExtensions`) and clean 1-line static delegating shortcuts on `AmbientContext` (`Services`, `WithServices`, `User`, `WithUser`, `CancellationToken`, `WithCancellationToken`, `WithTimeout`, `Blobs`, `WithBlobManager`, `LoggerFactory`, `WithLoggerFactory`, `Log<T>()`).
  - Accessing `AmbientContext.Services` outside an active scope throws `InvalidOperationException` to enforce explicit scoping.
  - Enabled `<Nullable>annotations</Nullable>` across all 24 `.csproj` files to allow modern `?` nullability documentation without compiler warning noise.
- **Consequences:** Highly ergonomic Developer Experience (DX) for both DI and zero-DI callers, zero code duplication, strict scoped resource safety, and unified compiler behavior across all projects.

## 2026-08-05: ADR-017: ActDim.BytePath: A registry record without content is a transient state, not a valid blob
- Date: 2026-08-05
- Status: accepted
- Context: The registry (`blob_records`) and the data store (files) are separate layers, so a record can exist while its content does not: either because `TryGetOrSetAsync` only registers the record and the caller never writes, or because the file was removed out of band. `VerifyExistsAsync` treated that state as `KeyNotFound` and deleted the record, which silently destroyed metadata and made the whole `TryGetOrSetAsync` → write flow fail on any test that skipped the write step.
- Decision: Every entry point now runs through `ReconcileContentAsync(result, allowNew, timeout, ct)`: named for what it does, since it mutates both the result and the registry rather than merely checking:
  - `TryGetOrSetAsync` (all four overloads) passes `allowNew: true`. A missing blob is reported by setting `BlobResult.IsNew = true` on the result whose lock we already hold: the caller learns the content has to be produced, and no record is lost.
  - `TryGetForReadingAsync` / `TryGetForWritingAsync` pass `allowNew: false`. They were asked for existing content, so the orphaned record is deleted and `KeyNotFound` returned. An `IsNew` result reaching them is an impossible state and throws `InvalidOperationException`.
  - The orphan deletion honours the caller's timeout via the new `IBlobRegistry.DeleteAsync(key, timeout, ct)` overload; a `TimeoutException` from it is converted to `BlobErrorCode.Timeout`, never swallowed into `KeyNotFound`: a lock held by someone else means we could not establish whether the record is orphaned at all, and the caller deserves a reason to retry.
- Consequences:
  - `BlobResult.IsNew` now means "there is no content yet", covering both a fresh record and one that outlived its blob. Its constructor became `internal` and the setter `internal` so only the library can produce results.
  - Callers must write content before relying on `TryGetFor*`; tests seed via `TestEnvironment.SeedAsync`.
  - Ported from `CanarySystems.FileStorage` but reimplemented: the original released the lock and re-acquired it (a race window), set `IsNew` on results that could be `Timeout`/`KeyNotFound`, and threaded a `timeout` parameter that every call site passed as `null`.
  - `IBlobManager.DeleteAsync` gained a `timeout` overload as well, so every public entry point that locks a specific key now exposes one. `DeleteExpiredAsync` / `DeleteOlderThanAsync` / `CleanupAsync` deliberately do not: they work through SQL conditions without per-key locks.


## 2026-08-05: ADR-018: ActDim.BytePath: `BlobRecord.Size` is owned by the library and read from the data store, not the registry
- Date: 2026-08-05
- Status: accepted
- Context: `Size` was mapped to and from the `size` column but never computed anywhere, so it stayed `NULL` forever unless a caller assigned the field by hand. That is not merely cosmetic: `AppendAsync(offset)` requires `offset == length` to append, and the length has no other source, so an unpopulated `Size` makes appending impossible. A stale value is worse still: after the record outlives its content, the size of the vanished blob would be reported as current.
- Decision:
  - `IBlobDataStore.ExistsAsync` is replaced by `Task<long?> GetSizeAsync(record, ct)`, where `null` means "no content". Existence and size come from a single round trip, which also suits object stores where one `HEAD` answers both. **Amended later the same day**: `ExistsAsync` came back as a *default interface implementation* returning `GetSizeAsync(...).HasValue`, for callers that only want the fact. `GetSizeAsync` stays the single primitive a store implements, so the two cannot disagree and no backend is tempted to spend a second round trip on existence.
  - `null` versus `0` is load-bearing: a size of `0` is an existing zero-byte blob, which a caller can legitimately produce by opening `WriteAsync` and writing nothing. Conflating the two would make `TryGetFor*` prune the record of an empty blob.
  - `ReconcileContentAsync` assigns `record.Size` from the data store on every hand-out. Because the record is handed out under a lock held until dispose, that value is authoritative for the lifetime of the handle, not a best-effort snapshot.
  - `TrackSizeOnDispose` chains `record.OnDisposeAsync` for write-locked records to re-read the size before the registry persists the record, so the `size` column matches reality for readers that never take a handle (`QueryAsync` and future reporting). Read locks are skipped: content cannot change under one.
  - `BlobRecord.Size` setter became `internal`: the library observes the size, callers do not declare it. `Hash` stays caller-declared via `BlobStoreOptions` because computing it means reading the whole blob, which must not happen implicitly on every write.
- Consequences:
  - The dispose-time refresh depends on the caller disposing the write stream before the record. That is the documented usage pattern but cannot be enforced through the current `Stream`-returning API.
  - `Hash` is still never computed from content; `FileSystemBlobDataStore.ComputeXxHash3Async` remains commented out.
  - Discovered while doing this: `DeleteAsync` removes the registry row and the locks but nothing ever deletes the stored bytes: `IBlobDataStore` has no delete operation at all. Recreating the same key then reports `IsNew` while a stale file sits on disk. Needs a separate task.


## 2026-08-05: ADR-019: ActDim.BytePath: `BlobRecord` stays decoupled from the streams handed out for it
- Date: 2026-08-05
- Status: accepted
- Context: Both the write lock and `Size` (#002) are only correct if the caller disposes the write stream before the record. That could be enforced by letting the record own the streams it was used to open and closing them in `BlobRecord.DisposeAsync`, which would make lock and size correct by construction.
- Decision: Do not do that. `BlobRecord` remains a lightweight near-POCO; stream lifetime is a separate concern owned by the caller. The ordering stays a documented convention.
- Consequences:
  - Both idiomatic forms already produce the right order: a nested `await using` block, and `using` declarations in one scope (disposed in reverse order of declaration). Breaking it requires deliberately disposing the record while a stream is open.
  - The failure mode is not primarily a wrong `Size`: disposing the record releases the distributed lock, so writing after that point is a correctness bug regardless. `Size` merely turned a silent race into a visible wrong number.
  - Do not "fix" this later by giving `BlobRecord` a collection of open streams: that was considered and rejected here.


## 2026-08-05: ADR-020: ActDim.BytePath: Deletion is orchestrated by `BlobManager`, content before metadata
- Date: 2026-08-05
- Status: accepted
- Context: `IBlobDataStore` had no delete operation, so every deletion path removed metadata and left the bytes on disk forever: unbounded growth, worst under TTL/sliding-expiration cache usage where records expire constantly. Recreating a deleted key then reported `IsNew` while stale content sat on disk. See task `delete-blob-content`.
- Decision:
  - `IBlobDataStore.DeleteAsync(record, ct)` requires a write lock, like every other mutating operation, and returns whether anything was removed. `FileSystemBlobDataStore` also prunes the shard directories it empties.
  - `BlobManager` owns all four deletion paths, being the only layer that sees both the registry and the data store. The registry lost its key-based `DeleteAsync` / `DeleteExpiredAsync` / `DeleteOlderThanAsync` and gained `DeleteLockedAsync(record)` (deletes rows while the caller holds the write lock, attested by `record.LockType`), `ForceUnlockAsync(key)`, and two candidate-selection queries.
  - Order is content first, then metadata: a leftover registry row is recoverable: #001 reports it as `IsNew` and `TryGetFor*` prunes it: whereas a leftover file is invisible to the library.
  - Bulk deletion selects candidates with the registry's existing lock-excluding SQL, then runs each key through the same single-key routine with a zero lock timeout, so a key locked since selection is skipped rather than waited on. `forceDeleteLocked` breaks existing locks first.
  - This required fixing what a zero timeout means. The acquisition loop treated `timeout <= TimeSpan.Zero` as "unspecified" and substituted the default, so "do not wait" was originally expressed as 1 ms: which also only worked probabilistically, since an attempt finishing in under a millisecond sent the loop through another 100 ms delay. Now only a **negative** value means unspecified, and `TimeSpan.Zero` attempts once and gives up by construction: the deadline is already reached when the first attempt returns. The lock's own TTL stays a separate concern with its 1 s floor.
- Consequences:
  - Bulk deletion is N lock acquisitions instead of one SQL statement. Accepted: the alternative deletes content out from under a live reader.
  - `DeleteAsync` still signals through exceptions (`KeyNotFoundException`, `TimeoutException`) rather than `BlobErrorCode`, because it returns `Task`. Consistent within itself, unlike the `TryGet*` family.
  - `ReconcileContentAsync` reuses `DeleteCoreAsync` for orphan pruning, so that path now cleans up content as well.


## 2026-08-05: ADR-021: ActDim.BytePath: The data store exposes two write operations, neither taking a position
- Date: 2026-08-05
- Status: accepted
- Context: `CreateAsync` (`FileMode.Create`) and `WriteAsync` (`FileMode.Truncate`) produced identical results and differed only in whether the file had to pre-exist, which forced the caller to know whether the key was new: the original trap that started this work. `AppendAsync(record, offset)` additionally required the caller to know the current size, and permitted `offset` past the end, which silently zero-fills a hole.
- Decision:
  - `CreateAsync` is removed. `WriteAsync` becomes `FileMode.Create`: create-or-truncate: so it is correct for a new and an existing key alike, and the caller never consults `IsNew` to pick a write method.
  - `AppendAsync` loses its `offset` parameter and uses `FileMode.Append`, which creates the file when absent and positions at the end. The store knows the size; the caller does not have to.
  - Rejected: merging both into `WriteAsync(record, offset)` with "discard whatever follows what I wrote". One rule, but it needs a wrapper stream calling `SetLength` on dispose, and it hides the capability difference behind a parameter value: an object-store backend can implement full replace (`PutObject`) while refusing append, which is legible in two methods and not in one.
  - Rejected: `WriteAtAsync` as a name for positioned writing: one letter apart from `WriteAsync` for very different semantics.
- Consequences:
  - Writing at an arbitrary position with the tail preserved is no longer possible. Resumable upload: the use case that motivated `offset`: still works: `record.Size` (#002) tells the caller how many bytes are already stored, and the rest is a plain append. Patching the middle of a blob would have to come back as its own explicit operation.
  - `FileMode.Truncate`'s implicit "must exist" assertion is gone. It was redundant on the sanctioned path anyway, since `ReconcileContentAsync` already establishes existence under the lock.
  - The write surface is now `WriteAsync` / `AppendAsync` / `ReadAsync` / `DeleteAsync` / `GetSizeAsync` / `ResolveLocationAsync`, with no mode flags or enums: the operation is the method. Their direction was inverted afterwards by #006.


## 2026-08-06: ADR-022: ActDim.BytePath: Options are instructions, the record is state; `Apply` moves to `BlobRecord`
- Date: 2026-08-06
- Status: accepted
- Context: `BlobRecord` had public setters for everything, which looked like a hole next to `Size` being `internal` (#002). It is not: a write lock is exclusive, so mutating a record you hold and having it persisted on dispose is precisely what the lock is for. What was actually wrong was an asymmetry: `BlobStoreOptions` could be applied through `TryGetOrSetAsync` but not through `TryGetForWritingAsync`, even though the lock is the same, so changing an existing blob's content type meant a needless get-or-set.
- Decision:
  - **Setter visibility follows what the value is, not whether mutation is safe.** Facts the library observes are `internal`: `Size`, `Hash` (once computed: see `content-hash`), `CreatedAt`/`UpdatedAt`/`AccessedAt`, and `Key`. Intent the caller declares stays public: `ContentType`, `Metadata`, `SlidingExpiration`, `ExpiresAt`. A caller can only lie about the first group; the library cannot derive the second.
  - `ApplyOptions` moves out of `SQLiteBlobRegistry` and becomes `BlobRecord.Apply(BlobStoreOptions)`, public and requiring a write lock. The registry keeps an `internal Apply(options, now)` overload: it applies options while a record is still being set up and `LockType` is not decided yet, and it passes its own `now` so every timestamp derived in one operation agrees.
  - `BlobStoreOptions` is therefore **not** a creation-time convenience: it is the instruction type. `Ttl` is relative and has no representation on the record; "apply only what was set" and the AbsoluteExpiration > Ttl > SlidingExpiration priority are rules, not assignments. Dropping options in favour of plain setters would push the instruction-to-state translation onto the caller.
- Consequences:
  - Metadata on an existing blob is now updated under the write lock it was handed out with. `TryGetOrSetAsync`'s options parameter is, in effect, sugar for get-or-create plus `Apply`.
  - Applying options is no longer the registry's business, which is right: it never was persistence logic, only computation over a record plus the current time.
  - Pre-existing hole left untouched: `UpdateOnReadDisposeAsync` also persists the record, so mutations made under a **read** lock reach storage even though other readers may hold the same key. The public `Apply` guards against it; the plain setters do not. Worth closing separately.


## 2026-08-06: ADR-023: ActDim.BytePath: Writes consume a stream; reads hand one out
- Date: 2026-08-06
- Status: accepted
- Context: `WriteAsync` / `AppendAsync` returned a writable stream for the caller to push into. That made the content non-existent until the caller disposed that stream, which is what forced the disposal-order convention (#003) and the dispose-time size refresh (#002). On an object store it is worse than a wrong `Size`: a multipart upload does not exist until `CompleteMultipartUpload`, so a mis-ordered dispose means a **missing object with the lock already released**, plus incomplete parts billing until aborted.
- Decision: invert the two write operations to consume a stream and return the resulting total size:
  ```csharp
  Task<long>   WriteAsync(record, Stream content, ct);
  Task<long>   AppendAsync(record, Stream content, ct);
  Task<Stream> ReadAsync(record, ct);   // unchanged
  ```
  Reading keeps handing a stream out. The asymmetry is deliberate and mirrors every storage SDK: `GetObject` returns a stream, `PutObject` accepts one; a consumer reads at its own pace while a producer hands over its source.
- Consequences:
  - The write is complete when the call returns. No disposal order to get wrong, no window where the content does not exist, and the size is known exactly once at the moment the bytes land: hence the `long` return.
  - `#003` was the right call but the problem it managed is largely gone: with no returned write stream there is nothing to mis-order. Its rule still applies to `ReadAsync`.
  - `TrackSizeOnDispose` (#002) **stays**. `FileSystemBlobDataStore` now records `record.Size` as it writes, but `BlobRecord.Size` has an `internal` setter, so an `IBlobDataStore` implemented in another assembly cannot. The dispose-time refresh is what keeps the persisted column right for any backend.
  - Opens the way to computing `Hash` while the bytes stream through, which #002 left undone because there was no single place that saw them.
  - Cost: a producer whose API only writes (`JsonSerializer.SerializeAsync`, `XmlWriter`, `GZipStream` compress) needs a `System.IO.Pipelines` bridge. This is ergonomics only: back-pressure and memory behaviour are unaffected, since the store pulls at the source's rate. That bridge ships as a producer-delegate overload of `WriteAsync`/`AppendAsync`. **Amended the same day**: it started as extension methods but became **default interface methods** on `IBlobDataStore`, because an extension is static and a backend could not specialise it: worse, a call through `IBlobDataStore` would silently pick the extension over a same-named instance method. `ProducerStreamBridge` (internal) holds the pipe default; `FileSystemBlobDataStore` overrides both and hands its own `FileStream` over, since bridging buys nothing when the store already owns a writable destination. Consequence: the supplied stream's seekability now varies by store, so the contract promises only that it is write-only, and `WriteThroughAsync` reports `file.Length` rather than `file.Position` because a producer holding the real file stream may seek.
  - Rejected: also putting `OpenWriteAsync` on `IBlobDataStore` (Azure ships both directions). It would reintroduce the "which write method?" fork that #005 removed. Add it only for a concrete large write-only producer on a backend with a native push API, where routing through our pipe would be pure overhead.


## 2026-08-06: ADR-024: ActDim.BytePath: `TryGetForWritingAsync` takes `BlobStoreOptions`, applied on the handle and persisted on dispose
- Date: 2026-08-06
- Status: accepted
- Context: #007 made `BlobRecord.Apply` public so an existing blob's metadata could be changed under the write lock it was handed out with, but the acquiring call still did not accept options: only `TryGetOrSetAsync` did. Every "update this existing blob" call site therefore read as acquire-then-apply, while the create-or-update path read as one call, for no reason other than which overload existed.
- Decision: add `TryGetForWritingAsync(key, options, ct)` and `(key, options, timeout, ct)`, ordering the parameters as `TryGetOrSetAsync` does. `BlobManager.ApplyOptions` calls `record.Apply(options)` on a successful result and returns a failed one untouched: a null `options` is also a no-op, so the new overloads are exactly the existing ones plus an `Apply`.
- Consequences:
  - **The options are persisted on dispose, not immediately.** `TryGetOrSetAsync` writes them straight away because it may release the write lock and re-acquire a read one, so it has to persist while it still holds the write lock; `TryGetForWritingAsync` holds that lock for the handle's lifetime and dispose persists the record anyway. A caller that abandons the handle without disposing it persists nothing: the same as for any other mutation made through it.
  - No change to `IBlobRegistry`: applying options is computation over a record (#007), so `BlobManager` is the right place and the registry surface stays as it was.
  - Not extended to `TryGetForReadingAsync`: `Apply` requires a write lock, and the read-dispose hole noted in #007 is a reason to keep it that way rather than to widen it.
  - `TryGetOrSetAsync` remains the only entry point that may create the key. The new overloads still return `KeyNotFound` for a missing record or one whose content is gone (#001).


## 2026-08-11: ADR-025: ActDim.BytePath: Whole-object writes are puts; resumable uploads are sessions
- Date: 2026-08-11
- Status: accepted
- Context: `WriteAsync` meant a whole-blob create-or-replace even though "write" naturally suggests an in-place or positioned change. A proposed `WriteAsync(record, content, offset)` would let a file-system backend assemble chunks out of order, but file length cannot prove that every range was received: writing a late chunk first creates holes. Exposing temporary content under the final key would also let readers observe an incomplete upload.
- Decision:
  - Rename both whole-object `WriteAsync` overloads to `PutAsync`; they retain `FileMode.Create` semantics and consume a source stream or producer delegate.
  - Do not add public positioned-write overloads to `IBlobDataStore`.
  - Model resumable out-of-order upload as a durable multipart upload session with begin, part upload, complete, and abort operations. The session owns staging content and received-range tracking; only successful completion publishes content at the final key.
- Consequences:
  - The rename is a breaking change to the public data-store interface, but removes the semantic ambiguity before consumers depend on it.
  - `AppendAsync` remains a separate operation; it is not an upload-completeness protocol.
  - Session design must specify idempotency, overlaps, expected length, expiry cleanup, atomic publication, and checksum behaviour before implementation. `content-hash` must be reconciled with that design.


## 2026-08-11: ADR-026: ActDim.BytePath: Multipart completion never overwrites a blob
- Date: 2026-08-11
- Status: accepted
- Context: A multipart upload stages data outside its final key, so completion must decide what to do if another writer has published that key while the session was active.
- Decision: `CompleteUploadAsync` reports a conflict when final content exists. It leaves that blob and the session's staged data unchanged, allowing the caller to abort or inspect/retry according to its own policy.
- Consequences: Completion must acquire the final key's write lock and check content existence while holding it. The API needs an explicit conflict result or exception; it must not silently replace the published blob.


## 2026-08-28: ADR-027: ActDim.BytePath: URL-safe key separator and configurable FileSystem hierarchy
- Date: 2026-08-28
- Status: accepted
- Context: `FileSystemBlobDataStore.BuildPath` treated `/` as a directory separator, which broke single-segment route matching in REST APIs (`/blobs/{key}`) and conflated public identifiers with on-disk folder layouts. Furthermore, DOS/Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9`) would fail or hang Windows filesystem operations.
- Decision:
  - Standardize on `:` (colon) as the default logical separator for hierarchy and prefixes (e.g. `fs:`, `s3:`, `tenant:folder:file.png`), which is URL-safe per RFC 3986 `pchar`.
  - Add `FileSystemBlobDataStoreOptions.HierarchySeparator` (char?, default `':'`). When set, multi-segment keys split by that separator into physical subdirectories; single-segment keys use uniform 2-level `XxHash3` sharding. When set to `null`, hierarchy is disabled and all keys are hash-sharded.
  - In `EscapeFileName`, detect Windows reserved device names (with or without extensions) and escape the leading character (e.g. `con.txt` -> `%63on.txt`) to avoid Win32 device handle traps.
- Consequences:
  - `/` is no longer a magic directory character in `FileSystemBlobDataStore`; it is escaped as `%2F` unless configured otherwise.
  - Keys with `:` round-trip through REST endpoints cleanly without route wildcard workarounds.
  - Windows reserved names are safely handled across all file operations.


## 2026-08-06: ADR-028: ActDim.BytePath: Multiple `IBlobManager` instances with self-describing key prefixes
- Date: 2026-08-06
- Status: proposed
- Context: `IBlobManager` holds exactly one `IBlobDataStore`. An app needs both FileSystem (for user uploads) and S3 (for cache). No way to express that.
- Decision:
  - Each `IBlobManager` registers normally in DI. Each knows its `KeyPrefix`.
  - `BlobManagerManifest` holds `Name`, `KeyPrefix`, `DataStore`, `Manager`, and `ResolveKey(key)`.
  - Client iterates all manifests, calls `ResolveKey`, picks the first match.
  - No routing inside `BlobManager`: the client decides.
  - Keys carry a prefix: `fs:my-blob`, `s3:my-blob` (aligned with `#011`).
- Consequences:
  - Zero coupling between backends: each is independent.
  - Client code knows which backend it wants.
  - Key format changes: every key carries a prefix.
  - Catch-all (empty prefix) matches everything; document that it should be last.


## 2026-08-04: ADR-029: ActDim.Practix.Common: Compression coverage is BCL-only; declared-but-unsupported formats throw
- Date: 2026-08-04
- Status: accepted
- Context: `CompressionFormat` declares GZip, Brotli, Deflate, BZip2, LZMA, LZMA2, PPMd and `ArchiveFormat`
  declares Zip, SevenZip, Rar, Tar. The .NET 10 base class library ships codecs for only GZip / Deflate /
  Brotli / ZIP / TAR. Covering the rest means taking a third-party dependency (SharpCompress or similar).
- Decision: implement `CompressionManager` against the BCL only. Signature *detection* covers every format we
  can recognize (BZip2, LZMA, 7z, RAR included), but any attempt to actually compress/decompress an
  unsupported format throws `NotSupportedException` naming the supported set. No third-party codec is added to
  `ActDim.Practix.Common`.
- Consequences: `ActDim.Practix.Common` keeps its current dependency set. Detection can legitimately return a
  format that the same object cannot process: deliberate: "what is this?" and "can I read it?" are different
  questions. Adding a codec later means a subclass or a sibling implementation, not a rewrite: the
  encoder/decoder factories (`CreateCompressionStream` / `CreateDecompressionStream`) are the single
  extension point.


## 2026-08-04: ADR-030: ActDim.Practix.Common: Stream ownership and rewind contract
- Date: 2026-08-04
- Status: accepted
- Context: the legacy (commented-out) code rewound `outputStream` to 0 even when the caller had supplied it,
  which silently breaks append/compose scenarios, and it was unclear who disposes what.
- Decision: (a) a stream `CompressionManager` CREATES is returned rewound to 0 and is owned by the caller
  (dispose returns the pooled blocks); (b) a destination the CALLER supplies is written from its current
  position and is never rewound and never closed: except in `CompressToArchiveAsync(Stream outputStream, ...)`,
  whose declared contract is to hand that stream back, so it is rewound; (c) an input stream is always
  consumed as a whole (rewound first when seekable) and left open; (d) entry streams handed to a
  reader/writer callback are owned by the manager and disposed as soon as the callback returns, and may be
  opened at most once per entry (`InvalidOperationException` otherwise): mandatory for ZIP, where an entry is
  only finalized when its stream closes.
- Consequences: callbacks must consume/write entry data before returning (documented on the members);
  composing several writes into one caller stream works; on failure the manager disposes any stream the caller
  will never receive, so pooled blocks are not leaked.


## 2026-08-04: ADR-031: ActDim.Practix.Common: `using` declarations, not `using` statements, for resource scoping
- Date: 2026-08-04
- Status: accepted
- Context: `CompressionManager` had up to four nested `using (...) { }` blocks (archive → entry stream → source
  stream), pushing real logic 5-6 levels deep for no benefit. The root `AGENTS.md` rule "always brace
  `if`/`else`/`for`/`foreach`/`while`/`do`/`using`: never single-line or same-line bodies" is about statement
  BODIES; a `using` *declaration* (`using var x = ...;`) has no body at all, so it is not what that rule targets.
  `Extensions/StreamExtensions.cs` already used the declaration form.
- Decision: prefer the declaration form `using var x = ...;` / `await using var x = ...;` for resource scoping.
  Keep the block form only where the resource must be released before code that follows it in the same scope
  (i.e. where disposal order genuinely differs from "end of enclosing block").
- Consequences: the whole of `CompressionManager` is now at most two levels deep. Two things to keep in mind:
  (a) inside a loop body a `using` declaration disposes at the END OF EACH ITERATION, which is what the ZIP
  writer relies on (an entry is only finalized when its stream closes, before the next `CreateEntry`);
  (b) multiple declarations in one scope dispose in reverse declaration order: same as nesting.
  Do not "restore" the braces citing the general bracing rule; that rule is about bodies.


## 2026-08-17: ADR-032: ActDim.Emitron: Standardize Default Parameter Name to @params
- **Status**: Accepted
- **Context**: Need a default parameter variable name in Roslyn scripts that is 100% collision-free with local C# script variables.
- **Decision**: Use `@params` as default parameter variable name. Because `params` is a reserved C# keyword, users cannot declare local `var params = ...` variables, eliminating collision risks.


## 2026-08-18: ADR-033: ActDim.Emitron: Support Assemblies and Usings via Script Directives and EmitronOptions
- **Status**: Accepted
- **Context**: Scripts and host applications need flexible mechanisms to reference assemblies and import namespaces both inline in script source and programmatically via options.
- **Decision**: Standardize on native C# nomenclature: `Assemblies` (and `AssemblyReferences`) for assembly references, and `Usings` for namespace imports. Support `#r "AssemblyName"` and `using Namespace;` directives with auto-injection of parameters after header directives, and provide `EmitronOptions` with `SearchPaths`.


## 2026-08-26: ADR-034: ActDim.Emitron.Razor: Dedicated Razor Engine Module
Created `ActDim.Emitron.Razor` as a separate assembly referencing `ActDim.Emitron` to keep `ActDim.Emitron` core lightweight while providing Razor syntax template compilation.

