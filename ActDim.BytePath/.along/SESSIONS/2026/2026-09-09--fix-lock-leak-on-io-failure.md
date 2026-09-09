---
protocol: along
date: 2026-09-09
slug: fix-lock-leak-on-io-failure
agent: antigravity
branch: main
summary: Fixed write lock leak in BlobManager.TrackSizeOnDispose when dataStore.GetSizeAsync throws on disposal
issues_advanced: []
issues_completed: [bug--fix-lock-leak-on-io-failure]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Fix Write Lock Leak in BlobManager On Disposal Error

## Context & Problem
In `ActDim.BytePath/BlobManager.cs`, `TrackSizeOnDispose` wraps `record.OnDisposeAsync` to refresh the content size from the data store for write-locked blobs. Previously, `releaseAsync()` was called directly after `await dataStore.GetSizeAsync(record, CancellationToken.None)` without a `finally` block.
If `GetSizeAsync` threw an exception (e.g. disk failure, I/O error), `releaseAsync()` was skipped, leaving the write lock permanently acquired in the registry.

## Changes Made
- In `ActDim.BytePath/BlobManager.cs`:
  - Wrapped `dataStore.GetSizeAsync` in `try ... finally` to guarantee invocation of `releaseAsync()` during disposal even when `GetSizeAsync` fails.
- In `Tests/BytePath.Tests/BlobManagerTests.cs`:
  - Added unit test `DisposeAsync_ReleasesWriteLock_EvenWhenDataStoreGetSizeThrows` using a fault-injecting `FaultyDataStore` to verify write lock release after an I/O failure during disposal.

## Verification
- All 105 tests in `ActDim.BytePath.Tests` passed.

