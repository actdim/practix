# Vision

_North star: scope, boundaries, non-goals, roadmap. Evolves slowly; slims as features ship._

## Scope
- Centralized structured logging, Activity/tracing propagation, and metric reporting for Practix services.
- Zero-allocation high-throughput spans, safe object flattening, diagnostic analyzers.

## Non-goals
- Storage backend implementations (provided by consumers / standard OpenTelemetry exporters).

## Roadmap
- Fix object cycle protection and collection tag serialization.
- Message template analyzer for Roslyn.
- Text log trace context enrichment.
