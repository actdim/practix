---
protocol: along
date: 2026-09-23
slug: signoz-documentation
agent: Antigravity
branch: main
commit: pending
summary: Documented SigNoz APM integration in ActDim.Observability README with architecture placement, direct OTLP endpoints, and OpenTelemetry Collector configuration.
milestone: ""
issues_advanced: []
issues_completed: [docs--signoz-documentation]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: SigNoz APM Documentation & Architecture Alignment

## What Changed & Why
1. **Evaluated SigNoz Integration Strategy**:
   - Assessed architectural trade-offs: verified that `ActDim.Observability` already supports SigNoz out of the box through standard OpenTelemetry protocols without requiring proprietary C# packages.
   - Identified operational constraints: SigNoz requires a full Docker Compose ClickHouse stack (not a standalone binary like VictoriaLogs or OpenObserve), so dedicated local integration test runners would introduce heavy external infrastructure dependencies without architectural benefit.
2. **README Documentation Update**:
   - Added SigNoz to recommended open-source solutions list in `ActDim.Observability/README.md`.
   - Updated architecture and OpenTelemetry Collector Mermaid diagrams to include SigNoz as an all-in-one APM sink.
   - Added OpenTelemetry Collector exporter configuration (`otlp/signoz` on port 4317) for trace, log, and metric pipelines.
   - Added SigNoz setup and endpoint configuration guide under Section 4.

## Verification
- Executed full test suite via `.along/scripts/test.py`.
- All 729 tests passed across all 12 test projects with 0 errors.

## Files Touched
- `ActDim.Observability/README.md`
- `ActDim.Observability/.along/ISSUES/done/docs--signoz-documentation.md`
- `.along/ISSUES/done/docs--signoz-documentation.md`

