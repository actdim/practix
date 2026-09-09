---
protocol: along
date: 2026-09-09
slug: fix-sync-over-async-constructor
agent: antigravity
branch: main
summary: Fixed sync-over-async in SQLiteBlobRegistry constructor by switching to synchronous schema initialization
issues_advanced: []
issues_completed: [bug--fix-sync-over-async-constructor]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Fix Sync-over-Async in SQLiteBlobRegistry Constructor

## Context & Problem
In `ActDim.BytePath.SqliteRegistry/SQLiteBlobRegistry.cs`, the constructor called `EnsureSchemaAsync().GetAwaiter().GetResult()`.
This sync-over-async call risked deadlocking when instantiated in environments with single-threaded synchronization contexts (e.g. UI threads or thread pool saturation).

## Changes Made
- In `ActDim.BytePath.SqliteRegistry/SQLiteBlobRegistry.cs`:
  - Replaced `EnsureSchemaAsync().GetAwaiter().GetResult()` with pure synchronous `EnsureSchema()`.
  - Used `_dbSemaphore.Wait()`, `conn.Open()`, and `cmd.ExecuteNonQuery()`.
- In `Tests/BytePath.Tests/BlobManagerTests.cs`:
  - Added unit test `Constructor_UnderSingleThreadSynchronizationContext_DoesNotDeadlock` verifying that constructing `SQLiteBlobRegistry` inside a `SingleThreadSynchronizationContext` completes without deadlock.

## Verification
- All 106 tests in `ActDim.BytePath.Tests` passed.

