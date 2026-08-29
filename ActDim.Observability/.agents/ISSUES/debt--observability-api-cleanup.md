---
slug: observability-api-cleanup
type: debt
status: open
priority: low
created: 2026-08-15
updated: 2026-08-15
---

# Debt: Small API and Semantics Cleanups in Observability

## Description
Individually minor, collectively the difference between an API that explains itself and one that needs a guide. Re-verified against the `IObservabilityContext` / `ExportedKeys` refactor.

1. **Two dead helpers.** `ObservabilityTagNames.IsReserved` and `ObservabilityContextPropertyNames.IsControlKey` are declared and never called anywhere. `IsControlKey` lost its only caller when the bridge switched from "export everything except control keys" to the explicit `ExportedKeys` set; `IsReserved` lost its purpose when the `log.*` namespace shrank to a single counter. Remove them, or state what they are for.

2. **`IObservabilityContext.Properties` exposes control keys.** A consumer reading ambient values: the planned console spinner, for instance: sees `__Practix_SuppressConsole`, `__Practix_SuppressedProviders` and `__Practix_ExportedKeys` mixed in with the data. Expose a data-only view, or filter with the (currently unused) `IsControlKey`.

3. **`Push` does not guard the control-key prefix.** `Push("__Practix_SuppressConsole", true)` writes a pipeline switch through the data path and marks it for export. Validate the name, or document that control keys are reserved.

4. **`SuppressExternalScopes` is a no-op in the default configuration.** `EventObservabilityOptions.IncludeExternalScopes` now defaults to `false`, so there is nothing to suppress unless the option was turned on, and the context offers no way to turn it on for a scope. Either add the enabling counterpart or drop the method. This is also the last remaining naming inconsistency: `SuppressConsole` / `SuppressProviders` suppress *output to sinks*, while `SuppressExternalScopes` suppresses *export to the span*.

5. **Collisions between the two write paths are not counted.** `PushExported` writes straight to the `Activity`, while `EnrichSpanFromScope` writes through `TelemetryTagCollector`. A scope-state key that overwrites a property exported earlier is a silent loss the counter never sees, which is exactly what `log.collisions` exists to prevent.

6. **`LogEvent.Name` is `null!` with a public setter.** An event can be constructed without a name, producing a placeholder span name later. Validate in the constructor or make the name required.

7. **`_scopeProvider` is written and read without a memory barrier** in `EventObservabilityBridge`. Benign in practice, formally a race.

8. **An undisposed scope leaks the span.** `logger.BeginScope(...)` without `using` leaves the auto-created activity current for the rest of the asynchronous flow. Nothing can fully prevent it, but it deserves a line in the documentation.

## Acceptance
- [ ] Each point is either fixed or explicitly rejected with a reason recorded here.
