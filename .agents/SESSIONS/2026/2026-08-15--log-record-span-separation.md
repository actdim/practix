---
date: 2026-08-15
slug: log-record-span-separation
agent: claude-opus-5
branch: main
commit: pending
summary: Separated the two signals: Log produces a log record only, BeginScope owns the span; fixed low-cardinality span naming and made DTO flattening collisions observable.
---

# Session Log: Signal Separation Between Log Records and Spans

## What changed & why

Continuation of `2026-08-14--telemetry-tag-namespacing`. Two defects were measured, not assumed.

**1. Trace content depended on log sink configuration.** `Log` still wrote ambient context and the log event onto the span from behind `_inner.IsEnabled(logLevel)`. Probe with `SetMinimumLevel(Warning)`: a span whose scope logged only `Information`/`Debug` lost `tenant.id` and `status` completely, and a `LogDebug(ex, ...)` recorded no exception.

Rather than adding a second level threshold, the signals were separated (ADR-008): `Log` produces a log record only, `BeginScope` owns the span. The log record already carries native trace correlation, so nothing is lost. The single carve-out is `Activity.AddException`, kept ungated so failures never disappear from traces.

**2. Span names had unbounded cardinality.** The auto-created activity took `state.ToString()`, so `BeginScope("Processing order {OrderId}", 42)` produced a span named `Processing order 42`. Fixed by ADR-009: `LogEvent.Name` → `{OriginalFormat}` → state string → non-anonymous type name → `"Scope"`.

**3. Collisions during DTO flattening were invisible.** Found by a failing test: `Flatten` resolved duplicates internally with `result[key] = value`, so `log.collisions` never saw them: contradicting what the previous session claimed it covered. Added `EventObservabilityHelper.FlattenPairs`, a streaming variant that preserves duplicates; `Flatten` is now a thin dictionary wrapper over it, so its public behaviour is unchanged.

**4. The exception carve-out duplicated itself on every layer.** Probe over the ordinary catch / log / rethrow pattern across three layers of one operation produced three identical `exception` events on one span. Added `SpanExceptionRecorder` (ADR-010): a `ConditionalWeakTable` keyed by the exception instance tracks the spans it has already been recorded on. Verified that identity survives `throw;` and `await`, that wrapping yields a separate record, and that the probe now reports a single event.

## Files touched
- `ActDim.Observability/EventObservabilityBridge.cs`: `Log` reduced to log record + exception carve-out; `EnrichSpanFromScope`, `ResolveOperationName` added; `EnrichActivityFromLogCall` removed.
- `ActDim.Observability/SpanExceptionRecorder.cs` (new)
- `ActDim.Observability/EventObservabilityOptions.cs`: `RecordExceptionsOnSpan`.
- `ActDim.Observability/ObservabilityTagNames.cs`: `Message` / `Level` / `EventId` removed; only `Collisions` remains.
- `ActDim.Observability/EventObservabilityHelper.cs`: `FlattenPairs`.
- `Tests/Observability.Tests/ObservabilityTests.cs`
- `.agents/DECISIONS.md`, `.agents/ISSUES.md`, `.agents/ISSUES/*`, `.agents/CONTEXT.md`, `.agents/GLOSSARY.md`, `.agents/HISTORY.md`

## Decisions
- ADR-008: signal separation; supersedes the event-level part of ADR-005 and narrows ADR-006 to the exception carve-out.
- ADR-009: low-cardinality span names.
- ADR-010: an exception is recorded at most once per span.

## Issues advanced
- Closed: `bug--span-enrichment-gated-by-log-level`.
- Opened: `task--ambient-context-pushed-after-scope`.
- Narrowed: `feat--message-template-analyzer`: placeholders no longer reach the span, so `PXO001` was dropped and the remaining rules now target `BeginScope` state.

## Verification
Full solution builds, `Tests/Observability.Tests`: 21 passed, 0 failed. Three probes re-run against the new model: level independence confirmed (ambient present on a span whose logs are all filtered out, exception recorded from a filtered `LogDebug`), span names stable across calls (`Processing order {OrderId}` for orders 42 and 43), `LogRecord` content unchanged (caller data plus native `TraceId`/`SpanId`).

## Gaps / follow-ups
- Ambient properties pushed after `BeginScope` never reach the span: see the new issue. This is the sharpest edge of ADR-008.
- A log call made without any scope still produces no trace data; that is now the documented model rather than an accident.
- `ActivitySourceRegistry` never evicts, so `ActivitySource` names must be a bounded set: not yet stated in its doc comment.
- `DefaultActivitySourceName` comes from the entry assembly; consumers must register that name via `AddSource(...)` or `StartActivity` returns null and the whole mechanism silently does nothing.
