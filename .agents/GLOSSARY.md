# Glossary

_Domain terms. Add a term when you introduce or clarify it._

- **Observability context** — `IObservabilityContext`, the ambient telemetry state of the current asynchronous flow: status, progress, icon, tags, plus per-scope pipeline switches. Data properties are exported to the current span as they are set. Distinct from **call context** (`ICallContext`), which is a neutral ambient variable store with no telemetry meaning and serves as its backing storage. See ADR-011.
- **Signal separation** — `Log` produces a log record and never touches the span; `BeginScope` owns the span. A logged `Exception` is the single carve-out, reported via `Activity.AddException`. See ADR-008.
- **Span attribute** — describes the whole operation (`Activity.SetTag`, written once by `BeginScope`): ambient context, external scopes, scope state. Log call content lives in the log record instead, correlated by trace context.
- **Low-cardinality span name** — a span name identifies an operation and must not carry per-call values, so it is derived from `LogEvent.Name` or the raw message template, never from the formatted state. See ADR-009.
- **Reserved tag namespace (`log.*`)** — tag names owned by `EventObservabilityBridge` itself (`log.message`, `log.level`, `log.event.id`, `log.collisions`), separated from application data so placeholders cannot overwrite them. See `ObservabilityTagNames`.
- **Tag collision** — two writes targeting the same telemetry key within one log call, typically after `ToOtelName` normalization collapses different names (`{UserId}` and `{user_id}` both become `user.id`). Resolved by `TagCollisionBehavior` and always counted in `log.collisions`.
- **ActivitySource Registry** — thread-safe cache (`ActivitySourceRegistry`) ensuring long-lived singleton `ActivitySource` instances per unique source name to prevent memory and runtime listener registration leaks.
- **CallContext ActivitySource Override** — ambient execution property (`callContext.PushActivitySourceName(...)`) allowing an async scope to declare the source name for automatically created `Activity` spans.

