---
protocol: along
protocol_version: "2.2.18"
slug: caching-proxies
title: Resilient Caching Proxies
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [caching, memory-cache, distributed-cache, proxy, serialization]
---

# Resilient Caching Proxies

`ActDim.Practix.Caching` provides transparent caching decorators (`MemoryCachingProxy` and `DistributedCachingProxy`) that wrap synchronous and asynchronous functions with automatic cache lookups, serialization, and cache entry configuration.

---

## Memory Caching Proxy (`MemoryCachingProxy`)

Wraps `IMemoryCache` from `Microsoft.Extensions.Caching.Memory` to decorate function delegates:

```csharp
IMemoryCachingProxy proxy = new MemoryCachingProxy(memoryCache);

// Decorate an asynchronous lookup function
Func<string, Task<UserProfile>> cachedFetcher = proxy.Get<Task<UserProfile>>(
    async key => await database.GetUserAsync(key),
    new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) }
);

// Subsequent calls with same key resolve from memory cache
UserProfile user = await cachedFetcher("user_102");
```

### Async / ValueTask Interception
`CachingProxyHelper.GetAwaitableResultType` dynamically inspects the return type `T`:
- Synchronous `Func<string, T>`: Checks `TryGetValue(key, out val)`, caches on miss.
- Asynchronous `Func<string, Task<TResult>>`: Emits compiled async task wrapper.
- High-Performance `Func<string, ValueTask<TResult>>`: Emits specialized value-task wrapper to minimize state machine heap allocations.

---

## Distributed Caching Proxy (`DistributedCachingProxy`)

Wraps `IDistributedCache` and an `IBinarySerializer` (`ActDim.Practix.Abstractions.Serialization`) to provide binary-serialized distributed caching:

```csharp
IDistributedCachingProxy proxy = new DistributedCachingProxy(distributedCache, binarySerializer);

Func<string, Task<ProductCatalog>> cachedCatalog = proxy.Get<Task<ProductCatalog>>(
    async key => await remoteService.GetCatalogAsync(key),
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }
);

ProductCatalog catalog = await cachedCatalog("catalog_electronics");
```

### Execution Flow:
1. Calls `_cache.GetAsync(key)`.
2. On Cache Hit: Deserializes payload using `_serializer.Deserialize<TResult>(bytes)`.
3. On Cache Miss: Invokes underlying factory delegate, serializes output via `_serializer.Serialize(value)`, saves to `_cache.SetAsync(key, bytes, options)`.

---

## Dependency Injection Setup

Register proxies through Microsoft Dependency Injection:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMemoryCache();
    services.AddDistributedMemoryCache(); // or Redis/SQL Server

    services.AddMemoryCachingProxy();
    services.AddDistributedCachingProxy();
}
```

---

## Key Invariants

1. **Transparent Delegation**: The caller interacts with a standard `Func<string, T>` signature; caching is completely decoupled from business logic.
2. **Deterministic Serialization**: Payloads in `DistributedCachingProxy` are serialized using `IBinarySerializer` (e.g. MessagePack, Protobuf, or System.Text.Json binary wrapper).
3. **No Stale Exception Caching**: If the factory delegate throws an exception during invocation, no cache entry is written, allowing subsequent calls to retry immediately.

