# Glossary

_Domain terms. Add a term when you introduce or clarify it._

- **Observability context**: `IObservabilityContext`, the ambient telemetry state of the current asynchronous flow: status, progress, icon, tags, plus per-scope pipeline switches. Data properties are exported to the current span as they are set. Distinct from **call context** (`ICallContext`), which is a neutral ambient variable store with no telemetry meaning and serves as its backing storage. See ADR-011.
- **Signal separation**: `Log` produces a log record and never touches the span; `BeginScope` owns the span. A logged `Exception` is the single carve-out, reported via `Activity.AddException`. See ADR-008.
- **Span attribute**: describes the whole operation (`Activity.SetTag`, written once by `BeginScope`): ambient context, external scopes, scope state. Log call content lives in the log record instead, correlated by trace context.
- **Low-cardinality span name**: a span name identifies an operation and must not carry per-call values, so it is derived from `LogEvent.Name` or the raw message template, never from the formatted state. See ADR-009.
- **Reserved tag namespace (`log.*`)**: tag names owned by `EventObservabilityBridge` itself (`log.message`, `log.level`, `log.event.id`, `log.collisions`), separated from application data so placeholders cannot overwrite them. See `ObservabilityTagNames`.
- **Tag collision**: two writes targeting the same telemetry key within one log call, typically after `ToOtelName` normalization collapses different names (`{UserId}` and `{user_id}` both become `user.id`). Resolved by `TagCollisionBehavior` and always counted in `log.collisions`.
- **ActivitySource Registry**: thread-safe cache (`ActivitySourceRegistry`) ensuring long-lived singleton `ActivitySource` instances per unique source name to prevent memory and runtime listener registration leaks.
- **CallContext ActivitySource Override**: ambient execution property (`callContext.PushActivitySourceName(...)`) allowing an async scope to declare the source name for automatically created `Activity` spans.

### Subproject terms

- **Record**: the metadata row for a key in `blob_records`. Exists independently of the content:
- **Content**: the stored bytes, owned by `IBlobDataStore`. The registry never sees them.
- **Orphaned record**: a record whose content is absent. A transient state by decision #001:
- **Reconciliation**: `BlobManager.ReconcileContentAsync`: bringing a record and its actual content
- **`IsNew`**: "there is no content yet". Covers both a record the registry has just created and
- **Handle**: a successful `BlobResult` / `BlobRecord` pair that holds a lock until disposed. Its
- **Shard directory**: the two subfolders `FileSystemBlobDataStore` derives from the key's
- **Forced deletion**: `forceDeleteLocked`: break existing locks (`ForceUnlockAsync`) and delete
- **Producer form**: the `WriteAsync`/`AppendAsync` overload taking
- **Write-only producer**: an API that writes into a stream it is handed and offers no readable form
- **Bridge**: `ProducerStreamBridge`: the default behind the producer form, pairing a `Pipe`'s write
- **Compression format** (`CompressionFormat`): a codec applied to a single byte stream, with no file
- **Archive format** (`ArchiveFormat`): a container holding multiple named entries: ZIP, TAR, 7z, RAR. The
- **Archive entry** (`IArchiveEntry` / `ArchiveEntry`): one named item inside an archive: uncompressed size,
- **Entry type** (`ArchiveEntryType`): what an entry represents: `RegularFile` / `Directory` / `SymbolicLink` /
- **Buffer owner** (`IBufferOwner<T>`): a disposable handle over a rented buffer that carries the valid
- **Temp / scratch stream**: a pooled `RecyclableMemoryStream` from `MemoryManager.Default`, used wherever a
- **ScriptEngine**: Roslyn-based C# script compilation and evaluation engine.
- **Interpolator**: Compiles C# interpolated string expressions (`$"Hello, {Name}!"`) into cached formatters.
- **@params**: Collision-free C# script parameter variable binding caller properties in Roslyn script scope.
- **RazorParser**: High-performance transpiler converting Razor syntax templates into executable C# Roslyn scripts.
- **EmitronRazor**: Static facade for compiling and formatting Razor templates using `Emitron`.
