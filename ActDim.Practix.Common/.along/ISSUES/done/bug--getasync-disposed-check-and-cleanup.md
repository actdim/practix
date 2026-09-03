---
protocol: along
slug: bug--getasync-disposed-check-and-cleanup
type: bug
status: done
priority: high
created: 2026-08-31
updated: 2026-08-31
completed: 2026-08-31
agent: antigravity
tags: [pooling, async, disposal, lifecycle, resilience]
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Bug: Guard GetAsync Against Disposed Pool and Clean Up Factory Objects

## Problem
When `AsyncObjectPool<T>` was disposed, background or concurrent `GetAsync()` requests needed thorough guard checks:
1. If the pool is already disposed, `GetAsync()` should immediately throw `ObjectDisposedException` without attempting to acquire semaphore or create items.
2. If the pool is disposed concurrently while `_factory()` is actively creating an object, the newly created object would leak or be returned from a disposed pool.
3. If an object is discarded or disposed after pool disposal, calling `_semaphore.Release()` on a disposed `SemaphoreSlim` must be prevented.

## Solution
1. Added `ThrowIfDisposed()` helper to check `Volatile.Read(ref _disposed) != 0` and throw `ObjectDisposedException`.
2. Called `ThrowIfDisposed()` at the entry of `GetAsync()`.
3. In `GetAsync()`, inspected `_disposed` immediately after `await _factory()`: if disposed, immediately disposed the created object via `DisposeItemAsync(item)` and threw `ObjectDisposedException`.
4. Guarded `_semaphore.Release()` in `DiscardAsync` and `GetAsync` catch block to only release when `_disposed == 0`.
5. Added unit tests verifying `ObjectDisposedException` is thrown when calling `GetAsync` on a disposed pool and that objects created during concurrent disposal are cleaned up without leaking.

