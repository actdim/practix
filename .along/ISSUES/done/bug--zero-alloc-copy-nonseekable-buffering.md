---
protocol: along
protocol_version: "4.4.6"
slug: zero-alloc-copy-nonseekable-buffering
type: bug
status: done
completed: 2026-10-06
priority: high
created: 2026-10-06
updated: 2026-10-06
agent: antigravity
tags: [stream, memory, zero-alloc, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Fix Unbounded Buffering in ZeroAllocCopyTo for Non-Seekable Streams

## Problem Statement / Background
In `ActDim.Practix.Common.Extensions.StreamExtensions`, `ZeroAllocCopyTo` and `ZeroAllocCopyToAsync` buffered entire non-seekable streams into intermediate memory before writing out data. For continuous, compressed, or high-volume non-seekable streams (such as network or crypto streams), this behavior broke zero-allocation streaming contracts and risked `OutOfMemoryException`.

## Implementation Details
1. Updated `ZeroAllocCopyTo` and `ZeroAllocCopyToAsync` in `StreamExtensions.cs` to route non-seekable streams (`!src.CanSeek`) through chunked pooled copy logic (`PooledCopy` / `PooledCopyAsync`) instead of buffering through `RecyclableMemoryStream`.
2. Ensured rented memory chunks from `ArrayPool<byte>` are properly returned on completion, failure, or cancellation.
3. Added unit tests in `Tests/Common.Tests/Extensions/StreamExtensionsTests.cs` verifying chunked transfer without unbounded memory expansion on non-seekable stream mocks.

## Acceptance Criteria
- [x] Non-seekable streams are copied via streaming pooled chunk buffers rather than buffered into intermediate memory.
- [x] Zero-allocation memory pool contracts are respected with guaranteed return of rented byte arrays.
- [x] Stream copy behavior verified for seekable and non-seekable streams across synchronous and asynchronous invocations.
- [x] Regression tests in `StreamExtensionsTests` pass cleanly.
