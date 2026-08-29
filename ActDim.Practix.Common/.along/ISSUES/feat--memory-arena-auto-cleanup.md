---
protocol: along
slug: memory-arena-auto-cleanup
type: feat
status: open
priority: medium
created: 2026-08-20
updated: 2026-08-20
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Feature: Memory Arena Pattern with Automatic Cleanup Scope

## Goal
Implement a scoped `MemoryArena` / `MemoryScope` pattern in `ActDim.Practix.Common.Memory` to track all rented streams and memory buffers within an execution scope and automatically return/dispose any unreturned buffers upon scope `Dispose()`.

## Context & Rationale
When working with high-throughput zero-allocation pooling (`RecyclableMemoryStreamManager`, `ArrayPoolBufferOwner<T>`, `IBufferOwner<T>`), developers may occasionally forget to wrap rented streams or buffers in `using` statements, leading to memory leaks or pool exhaustion in background jobs or HTTP requests.

A RAII-style `MemoryArena` scope tracks all issued streams/buffers for the duration of a `using` block. Upon scope termination, any un-disposed buffers are safely returned to their respective pools.

## Key Considerations & Requirements
1. **Scope Tracking**:
   - Provide `AmbientContext.CreateMemoryArena()` or `MemoryArena.Create()` returning an `IDisposable` arena scope.
   - Track rented `RecyclableMemoryStream` instances and `IBufferOwner<T>` / `ArrayPoolBufferOwner<T>` handles.

2. **Auto-Cleanup on Scope Disposal**:
   - When the arena scope is disposed, automatically dispose/return all tracked memory objects that have not already been disposed.

3. **Escape / Detach Mechanism**:
   - Provide a `.Detach(stream)` or `.SuppressAutoDispose(stream)` mechanism so streams intended to escape the scope (e.g., returned as an HTTP response stream) are not prematurely disposed.

4. **Integration with Existing Memory Infrastructure**:
   - Leverage existing `IBufferOwner<T>`, `ArrayPoolBufferOwner<T>`, and `MemoryManager.Default` (`RecyclableMemoryStreamManager`).

5. **Unit & Integration Tests**:
   - Thoroughly test automatic disposal of forgotten streams, explicit `.Detach()`, and async execution flow propagation.
