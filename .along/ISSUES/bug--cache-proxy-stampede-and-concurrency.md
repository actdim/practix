---
protocol: along
slug: cache-proxy-stampede-and-concurrency
type: bug
status: open
priority: high
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [caching, concurrency, stampede, memorycache, distributedcache, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Cache Proxy Stampede and Concurrency Flaws

## Overview
`MemoryCachingProxy` and `DistributedCachingProxy` in `ActDim.Practix.Common/Caching/` exhibit cache stampede (thundering herd) vulnerabilities under concurrent access, lack atomic `GetOrCreate` semantics, miss asynchronous continuation optimizations, and perform redundant whole-payload serializations.

## Identified Defects

### 1. Cache Stampede and Race Conditions
In `MemoryCachingProxy.cs` (lines 41-48, 57-64, 73-80) and `DistributedCachingProxy.cs` (lines 45-53, 62-70):
```csharp
if (_cache.TryGetValue(key, out T cached))
{
    return cached;
}

var value = func(key);
_cache.Set(key, value, options);
return value;
```
The execution pattern is a naive check-then-act (`TryGetValue -> factory -> Set`). When multiple concurrent requests experience a cache miss for the same key, every caller executes the factory in parallel. For expensive calculations or database queries, this causes a thundering herd problem and repeated work.

### 2. Missing `ConfigureAwait(false)` in Async Paths
In `MemoryCachingProxy.cs` and `DistributedCachingProxy.cs`:
In `BuildTask` and `BuildValueTask`, `await func(key)` is awaited without `.ConfigureAwait(false)`. In synchronization context environments (UI, legacy ASP.NET), this risks thread starvation or deadlocks.

### 3. Full Payload Serialization on Every Miss in `DistributedCachingProxy`
Every miss results in calling `_serializer.Serialize(value)` directly on the calling thread, blocking or performing redundant heavy serialization when parallel misses happen.

## Remediation Plan
1. Introduce key-level single-flight locking or semaphore-per-key coordination (or integrate `GetOrCreateAsync` from `IMemoryCache` / hybrid cache) to ensure only one factory executes per key miss.
2. Add `.ConfigureAwait(false)` to all await points in caching proxy builders.
3. Coordinate distributed cache writes so that duplicate parallel misses do not repeatedly serialize identical values.

