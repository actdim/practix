---
slug: observability-package-guide
type: docs
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
---

# Docs: The Observability Package Has No Guide

## Description
`ActDim.Practix.Observability` has no README and no usage example. Everything a consumer needs to know lives in XML comments and in eleven ADRs — and the rules are not guessable from the API surface:

- a log call produces a log record and never shapes the trace (ADR-008), with the exception carve-out;
- `BeginScope` is what creates and enriches a span, and span names must stay low-cardinality (ADR-009);
- ambient telemetry state lives in `IObservabilityContext`, not in `IAmbientContext` (ADR-011), and is exported at push time;
- export is opt-in: only properties set through `IObservabilityContext` become span attributes. Anything pushed straight into `IAmbientContext` stays invisible to telemetry — a silent no-op for anyone who reaches for the ambient store directly;
- external scopes are off by default (`IncludeExternalScopes = false`);
- the consumer must register the `ActivitySource` name with `AddSource(...)`, otherwise `StartActivity` returns null and the whole mechanism silently does nothing;
- a log call made without any scope produces no trace data at all.

The last two produce complete silence rather than an error, which is the worst possible first-run experience.

## Proposal
A README next to the project with: a minimal `AddEventObservability` + OpenTelemetry wiring sample, the log/span ownership rule, the ambient context, the suppression switches, a table of the attributes the bridge emits, and a short troubleshooting section for "I see no traces".

## Acceptance
- [ ] README exists and a newcomer can wire the package up from it alone.
- [ ] The silent-failure cases are covered in troubleshooting.
- [ ] The root `README.md` links to it.
