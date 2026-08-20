---
slug: remove-memory-stream-manager-extensions
type: debt
status: done
priority: low
created: 2026-08-20
updated: 2026-08-20
---
# Remove MemoryStreamManagerExtensions Dead Code

Removed unused `MemoryStreamManagerExtensions` helper class containing `GetContextStream(...)` extension methods on `RecyclableMemoryStreamManager`.

## Rationale
1. `MemoryStreamManagerExtensions` had 0 call sites across the solution.
2. `GetContextStream` created a heavy `new StackTrace().GetMethod()` and introspection formatting on every call, causing reflection and heap allocations that defeated the purpose of pooled recyclable memory streams (`RecyclableMemoryStreamManager`).
3. Standard `RecyclableMemoryStreamManager.GetStream(...)` from `Microsoft.IO` already provides clean, high-performance stream acquisition methods.
