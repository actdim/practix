---
protocol: along
slug: multi-backend
type: task
status: done
priority: high
created: 2026-08-17
updated: 2026-08-17
completed: 2026-08-17
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# multi-backend: multiple stores with KeyPrefix routing

## Summary
`BlobManager` supports registering multiple `IBlobDataStore` implementations simultaneously. Each data store self-describes its handled key prefix (`KeyPrefix`). `BlobManager` routes blob operations (size reconciliation, read, delete, lifecycle) to the appropriate store based on key matching.

## Key format & Routing rules
- Keys carry an optional prefix: `fs:my-blob`, `cache:my-blob`.
- Specific (longest) non-empty prefix matches first.
- Empty prefix `""` acts as catch-all / default fallback.
- If no data store matches the key:
  - Methods returning `BlobResult` (`TryGetForReadingAsync`, `TryGetForWritingAsync`, `TryGetOrSetAsync`) return `BlobResult(BlobErrorCode.UnsupportedKeyPrefix)` without throwing exceptions.
  - `DeleteAsync(key)` and direct `GetDataStore(key)` throw `NotSupportedException`.

## DI Registration
```csharp
services.AddBlobManager()
    .WithFileSystemDataStore(@"D:\data", "fs:")
    .WithFileSystemDataStore(@"D:\cache", "cache:")
    .WithSQLiteRegistry(@"D:\data\registry.sqlite");
```
