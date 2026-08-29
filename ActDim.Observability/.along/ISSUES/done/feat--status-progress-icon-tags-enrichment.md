---
protocol: along
slug: status-progress-icon-tags-enrichment
type: feat
status: done
priority: medium
created: 2026-08-14
updated: 2026-08-14
completed: 2026-08-14
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Feature: Ambient Status, Progress, Icon & Tag Enrichment

## Summary
- Added `status`, `progress`, `icon`, and `tags` keys to `CallContextPropertyNames`.
- Implemented `callContext.SetStatus("Status", icon: "🚀")`, `callContext.ReportProgress(45.5)`, and `callContext.PushTags("billing")`.
- Verified OpenTelemetry Activity tag enrichment and unit tests passing.
