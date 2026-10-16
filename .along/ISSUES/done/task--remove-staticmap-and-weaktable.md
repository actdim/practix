---
protocol: along
protocol_version: "4.4.7"
slug: remove-staticmap-and-weaktable
type: task
status: done
completed: 2026-10-08
priority: medium
created: 2026-10-08
updated: 2026-10-08
agent: antigravity
tags: [cleanup, collections]
milestone: v2.0.0-along-transition
blocked_by: []
related: [bug--weak-table-custom-comparer-hashcode-mismatch]
---

# Remove Obsolete StaticMap and WeakTable

## Overview
Remove legacy, defective collection implementations:
1. `StaticMap.cs`: Outperformed and superseded by standard .NET `FrozenDictionary`.
2. `WeakTable.cs`: Legacy custom weak table with known comparer bugs, superseded by BCL `ConditionalWeakTable`.

## Acceptance Criteria
- [x] Remove `StaticMap.cs` and `WeakTable.cs`.
- [x] Remove `StaticMapTests.cs` and `StaticBiMapTests.cs`.
- [x] Update documentation references in `ActDim.Practix.Common`.
- [x] Reconcile `bug--weak-table-custom-comparer-hashcode-mismatch.md`.
- [x] Clean build and test execution.
