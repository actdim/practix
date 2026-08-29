---
protocol: along
slug: feat--large-payload-compression
type: feat
status: open
priority: medium
created: 2026-08-17
updated: 2026-08-17
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# feat: Implement streaming/spill-to-file compression for large payloads

## Problem
`CompressionManager` in `ActDim.Practix.Common` compresses payloads entirely in memory as `byte[]` buffers. For large payloads (multi-megabyte/gigabyte streams), this causes excessive LOH (Large Object Heap) allocations and potential OutOfMemoryException.

## Acceptance Criteria
- Implement a streaming/spill-to-file version of `CompressionManager` that compresses directly to a target `Stream` or temporary file without buffering full byte arrays in memory.
- Provide async API overloads: `CompressToStreamAsync` and `DecompressFromStreamAsync`.
- Add unit tests covering large payload streaming compression.
