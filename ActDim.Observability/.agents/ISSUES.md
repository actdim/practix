# Active Issues

## Active
- [feat--metrics-enrichment-and-timers](ISSUES/feat--metrics-enrichment-and-timers.md) — Metrics helper extensions (`MeasureDuration`, `TrackInFlight`) and low-cardinality ambient context tag filtering.
- [bug--collection-tag-values-not-exportable](ISSUES/bug--collection-tag-values-not-exportable.md) — a collection passed to `Push` or `LogEvent.ActivityTags` reaches the span as an object OTLP cannot express.
- [bug--logger-providers-registered-later-not-decorated](ISSUES/bug--logger-providers-registered-later-not-decorated.md) — DI registration is order-dependent and not idempotent.
- [bug--span-tag-restore-assumes-lifo](ISSUES/bug--span-tag-restore-assumes-lifo.md) — out-of-order disposal leaves a stale span attribute and desynchronizes `ExportedKeys`.
- [feat--span-status-and-kind](ISSUES/feat--span-status-and-kind.md) — failed operations look successful; span kind is always Internal.
- [debt--enrichment-ignores-sampling](ISSUES/debt--enrichment-ignores-sampling.md) — enrichment runs for non-recording spans; exported keys re-written on every scope.
- [debt--test-attribute-in-production-code](ISSUES/debt--test-attribute-in-production-code.md) — library matches `TestProviderAliasAttribute` by name.
- [task--attribute-naming-across-signals](ISSUES/task--attribute-naming-across-signals.md) — `OrderId` in logs vs `order.id` in traces; needs a recorded decision.
- [task--autocreate-activity-scope-modes](ISSUES/task--autocreate-activity-scope-modes.md) — scope-to-span behaviour depends on the caller; replace the bool with modes.
- [docs--observability-package-guide](ISSUES/docs--observability-package-guide.md) — no README; the silent-failure cases are undiscoverable.
- [feat--message-template-analyzer](ISSUES/feat--message-template-analyzer.md) — Roslyn analyzer (PXO002-005) plus `.editorconfig` escalation of CA2017/CA2253/CA2254.
- [feat--trace-context-in-text-logs](ISSUES/feat--trace-context-in-text-logs.md) — ActivityTrackingOptions so Console/File sinks carry TraceId/SpanId.
- [task--activity-source-registry-unbounded](ISSUES/task--activity-source-registry-unbounded.md) — dynamic source names leak ActivitySource instances for the process lifetime.
- [debt--observability-api-cleanup](ISSUES/debt--observability-api-cleanup.md) — two dead helpers, control keys in `Properties`, unguarded `Push`, `SuppressExternalScopes` no-op, uncounted cross-path collisions, and three more.
- [feat--interactive-console-context-spinner](ISSUES/feat--interactive-console-context-spinner.md) — Interactive console context animation, Braille spinners, status icons, and progress bars.

## Backlog

## Done (recent)
- [bug--unsafe-object-flattening](ISSUES/done/bug--unsafe-object-flattening.md) — **critical** — safe object graph flattening: cycles, throwing getters, attribute count limits, and root collection prefixes.
- [bug--log-event-tags-duplicated](ISSUES/done/bug--log-event-tags-duplicated.md) — `LogEvent` tags deduplicated and written under plain tag names.
- [task--ambient-context-pushed-after-scope](ISSUES/done/task--ambient-context-pushed-after-scope.md) — `IObservabilityContext` separated from `ICallContext`; properties export at push time and on the scope snapshot.
- [bug--span-enrichment-gated-by-log-level](ISSUES/done/bug--span-enrichment-gated-by-log-level.md) — Log produces a log record only; BeginScope owns the span, independent of level filtering.
- [task--logs-without-active-span](ISSUES/done/task--logs-without-active-span.md) — Auto-create Activity span on BeginScope with CallContext override and EntryAssembly default.
- [debt--telemetry-tag-namespacing](ISSUES/done/debt--telemetry-tag-namespacing.md) — Span/event split, reserved `log.*` namespace, collision policy and counter, `Activity.AddException`.
- [feat--unified-observability-bridge](ISSUES/done/feat--unified-observability-bridge.md) — Refactored Logging to ActDim.Observability and EventObservabilityBridge.
- [feat--dynamic-telemetry-and-provider-suppression](ISSUES/done/feat--dynamic-telemetry-and-provider-suppression.md) — Selective provider suppression (SuppressConsole, SuppressProviders) and ProviderAlias resolution.
- [feat--status-progress-icon-tags-enrichment](ISSUES/done/feat--status-progress-icon-tags-enrichment.md) — Ambient status, progress percentage, icon, and tags telemetry enrichment.
