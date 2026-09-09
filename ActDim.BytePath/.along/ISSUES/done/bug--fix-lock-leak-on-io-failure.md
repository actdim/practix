---
protocol: along
protocol_version: "2.2.26"
slug: bug--fix-lock-leak-on-io-failure
type: bug
status: done
priority: critical
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [bytepath, concurrency, locks, storage, reliability]
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Fix lock leak in BlobManager.TrackSizeOnDispose during I/O failure

## Problem
In `BlobManager.TrackSizeOnDispose`, when a write-locked record is disposed, it re-queries the content size via `dataStore.GetSizeAsync(...)` before calling `releaseAsync()`.
If `dataStore.GetSizeAsync` throws an exception (such as `IOException`, corrupted file, disk failure, network timeout),
the original `releaseAsync()` callback was skipped because it was not enclosed in a `finally` block.
This caused write locks in the registry to remain permanently acquired, blocking subsequent read and write operations indefinitely.

## Solution
1. Wrapped `dataStore.GetSizeAsync` in `try ... finally` inside `BlobManager.TrackSizeOnDispose` to ensure `releaseAsync()` is always executed even on failure.
2. Added a unit test in `ActDim.BytePath.Tests` verifying that when `GetSizeAsync` throws on disposal of a write-locked blob, the underlying write lock is reliably released and subsequent operations succeed.

