---
protocol: along
slug: metrics-enrichment-and-timers
type: feat
status: open
priority: medium
created: 2026-08-20
updated: 2026-08-20
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Feature: Metrics Helper Extensions & Low-Cardinality Context Enrichment

## Goal
Enhance `ActDim.Observability` with developer-friendly `System.Diagnostics.Metrics` value-adds while preventing metric cardinality explosion.

## Context & Rationale
Standard .NET `System.Diagnostics.Metrics` (`Meter`, `Counter<T>`, `Histogram<T>`) are high-performance but require verbose boilerplate to record duration histograms or active in-flight task gauges. Furthermore, attaching contextual tags (`tenant.id`, `env`) manually on every `.Add(1)` leads to code duplication. However, indiscriminately attaching high-cardinality ambient keys (`user.id`, `order.id`, GUIDs) to metrics causes severe Prometheus/TSDB metric cardinality explosion.

## Planned Features
1. **Low-Cardinality Ambient Context Tag Filtering (`MetricsAllowedContextKeys`)**:
   - Provide explicit whitelist options in `EventObservabilityOptions` (e.g., `tenant.id`, `environment`, `service.name`).
   - Automatically filter out high-cardinality keys (`user.id`, `order.id`, `request.id`) before metric recording.

2. **Zero-Ceremony Duration Timers (`MeasureDuration`)**:
   - Add `observability.MeasureDuration("operation.duration", unit: "ms")` returning an `IDisposable` timer scope.
   - Automatically records duration in a `System.Diagnostics.Metrics.Histogram<double>` upon dispose.

3. **In-Flight Task Counter Scopes (`TrackInFlight`)**:
   - Add `observability.TrackInFlight("background_jobs.active")` returning an `IDisposable` scope.
   - Increments an `UpDownCounter<long>` (+1) on entry and decrements (-1) on dispose.

4. **Unit & Integration Tests**:
   - Add test coverage verifying tag filtering, histogram duration recording, and in-flight counter increment/decrement cycles.
