---
slug: unified-observability-bridge
type: feat
status: done
priority: high
created: 2026-08-14
updated: 2026-08-14
---

# Feature: Unified Observability Bridge & Package Renaming

## Summary
- Refactored `ActDim.Practix.Logging` $\rightarrow$ **`ActDim.Observability`**.
- Created **`EventObservabilityBridge`** (`ILogger` & `ISupportExternalScope` decorator) and **`EventObservabilityLoggerFactory`**.
- Provided DI registration method **`services.AddEventObservability()`**.
- Verified clean build and 7/7 unit tests passing.
