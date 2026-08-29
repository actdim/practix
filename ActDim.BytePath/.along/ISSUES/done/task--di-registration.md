---
protocol: along
slug: di-registration
type: task
status: done
priority: high
created: 2026-08-06
updated: 2026-08-17
completed: 2026-08-17
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# di-registration

## Problem

Nothing outside the assembly could use this library. `BlobManager` was internal, there was no factory, and no container registration.

## Solution

1. Standardized completely on Microsoft Dependency Injection via `ServiceCollectionExtensions`.
2. Made `BlobManager` public implementing `IBlobManager`.
3. Created extension methods `services.AddBlobManager()`, `services.AddFileSystemBlobDataStore()`, and `services.AddSQLiteBlobRegistry()`.
4. Registered `IBlobRegistry` as a Singleton (with `SQLiteBlobRegistry` thread safety and lock serialization).
5. Deleted obsolete `BlobManagerModule.cs`.
6. Updated `README.md` status note and quick-start example.

## Done when

- [x] a consumer outside the assembly can obtain an `IBlobManager` without `InternalsVisibleTo`
- [x] registry registered as a singleton, with the reason documented at the registration site
- [x] `BlobManagerModule.cs` either implemented or deleted: not left commented out
- [x] `README.md`'s status note removed, and its quick-start example made real
