---
protocol: along
slug: debt--arraysegment-blockcopy-optimization
type: debt
status: open
priority: low
created: 2026-08-17
updated: 2026-08-17
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# debt: Evaluate Buffer.BlockCopy / MemoryMarshal fast path in ArraySegmentExtensions.CloneToArray

## Problem
`ArraySegmentExtensions.CloneToArray` currently uses `Array.Copy`. For primitive/unmanaged element types (`T : unmanaged`), using `MemoryMarshal.Cast` or `Unsafe.CopyBlock` / `Buffer.BlockCopy` could offer better throughput.

## Acceptance Criteria
- Benchmark `Array.Copy` vs `MemoryMarshal` / `Buffer.BlockCopy` for value type array segments.
- Add an unmanaged fast path if performance gains are significant.
- Add unit tests.
