---
protocol: along
slug: collection-tag-values-not-exportable
type: bug
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Bug: Collection Tag Values Are Not Exportable as OpenTelemetry Attributes

## Description
OpenTelemetry attribute values may be primitives, strings, booleans or **arrays** of those. Anything else is converted with `ToString()` by the OTLP exporter, so a collection reaches the backend as a type name instead of its contents.

Two paths still hand collections straight to `Activity.SetTag`:

1. `IObservabilityContext.Push(name, value)`: the value is written verbatim in `ObservabilityContext.PushExported` and again from the exported-keys pass in `EnrichSpanFromScope`. `Push("Labels", new HashSet<string> { ... })` produces `System.Collections.Generic.HashSet\`1[System.String]`.
2. `LogEvent.ActivityTags` values, written verbatim in `EnrichSpanFromScope`.

`EventObservabilityHelper.FlattenPairs` is not affected: it expands collections into indexed scalars.

In-process consumers see the real object through `Activity.GetTagItem`, which is why tests pass and the defect stays invisible until export.

## Note on scope
Narrowed after the `IObservabilityContext` refactor: `PushTags` and the `tags` well-known key no longer exist, so the previously reported `HashSet<string>` on every operation is gone. What remains is the generic path: any caller can still push a collection.

## Verification needed
The OTLP exporter package is not in the local NuGet cache, so this was reasoned from the transformer contract rather than measured. Confirm against `OpenTelemetry.Exporter.OpenTelemetryProtocol` before fixing, and keep the reproduction as a test.

## Proposal
Convert collection values to an array of primitives at the single point where a value becomes a span tag, so both write paths are covered. Reject or stringify what cannot be expressed, rather than emitting a type name.

## Acceptance
- [ ] A collection pushed through `Push` arrives at an OTLP backend as a list.
- [ ] A test asserts the exported attribute value type, not just the in-process one.
