---
protocol: along
protocol_version: "2.2.26"
slug: bug--fix-sync-over-async-constructor
type: bug
status: done
priority: high
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [sqlite, registry, concurrency, deadlock, sync-over-async]
---

# Fix sync-over-async in SQLiteBlobRegistry constructor

## Problem
In `SQLiteBlobRegistry.cs`, the constructor invoked `EnsureSchemaAsync().GetAwaiter().GetResult()`.
Inside `EnsureSchemaAsync`, asynchronous operations (`_dbSemaphore.WaitAsync()`, `CreateOpenConnectionAsync()`, and `conn.ExecuteNonQueryAsync()`) were executed.
When instantiated under a single-threaded `SynchronizationContext` or constrained thread pool context, blocking the calling thread with `.GetAwaiter().GetResult()` while waiting for async continuations risked thread deadlock and thread pool starvation.

## Solution
1. Replaced `EnsureSchemaAsync().GetAwaiter().GetResult()` in the constructor with a pure synchronous `EnsureSchema()` method utilizing native synchronous SQLite calls (`_dbSemaphore.Wait()`, `conn.Open()`, and `cmd.ExecuteNonQuery()`).
2. Added a test in `Tests/BytePath.Tests` verifying that constructing `SQLiteBlobRegistry` under a `SingleThreadSynchronizationContext` completes immediately without deadlock.

