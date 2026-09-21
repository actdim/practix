---
protocol: along
slug: json-di-registration-missing-interfaces
type: bug
status: in-progress
priority: high
created: 2026-09-15
updated: 2026-09-21
agent: antigravity
tags: [json, di, dependency-injection, servicecollection, caching]
milestone: v2.0.0-along-transition
blocked_by: []
related: [core-json-serializer-contract-and-merge-flaws]
---

# AddPractixJson DI Registration Missing Promised Interfaces

## Overview
`ServiceCollectionExtensions.AddPractixJson()` only registers `IJsonSerializer`. However, `CoreJsonSerializer` implements multiple serialization abstractions from `ActDim.Practix.Abstractions.Serialization`, and package documentation explicitly promises them. Downstream components such as `DistributedCachingProxy` depend on `IBinarySerializer` and fail when configured with standard DI setup.

## Identified Defects

### 1. Missing Interface Registrations in DI
In `Extensions/ServiceCollectionExtensions.cs` (lines 18-27):
```csharp
public static IServiceCollection AddPractixJson(this IServiceCollection services)
{
    if (services == null)
    {
        throw new ArgumentNullException(nameof(services));
    }

    services.TryAddSingleton<IJsonSerializer, CoreJsonSerializer>();
    return services;
}
```
In `CoreJsonSerializer.cs` (line 22):
```csharp
internal class CoreJsonSerializer : IJsonSerializer, IStringSerializer, IBinarySerializer, IStreamSerializer
```
`CoreJsonSerializer` implements:
- `IJsonSerializer`
- `IStringSerializer`
- `IBinarySerializer`
- `IStreamSerializer`

Only `IJsonSerializer` is added via `TryAddSingleton`.

### 2. Downstream Dependency Failure
In `ActDim.Practix.Common/Caching/DistributedCachingProxy.cs` (line 20):
```csharp
public DistributedCachingProxy(IDistributedCache cache, IBinarySerializer serializer)
```
When consuming applications call `services.AddPractixJson()` as documented in `ActDim.Practix.Json/README.md`, resolving `IDistributedCachingProxy` or `IBinarySerializer` throws `InvalidOperationException: Unable to resolve service for type 'ActDim.Practix.Abstractions.Serialization.IBinarySerializer'`.

## Remediation Plan
1. Update `AddPractixJson` to register `CoreJsonSerializer` as singleton and forward all implemented interfaces:
```csharp
services.TryAddSingleton<CoreJsonSerializer>();
services.TryAddSingleton<IJsonSerializer>(sp => sp.GetRequiredService<CoreJsonSerializer>());
services.TryAddSingleton<IStringSerializer>(sp => sp.GetRequiredService<CoreJsonSerializer>());
services.TryAddSingleton<IBinarySerializer>(sp => sp.GetRequiredService<CoreJsonSerializer>());
services.TryAddSingleton<IStreamSerializer>(sp => sp.GetRequiredService<CoreJsonSerializer>());
```
2. Add unit tests verifying that all four interfaces resolve to the same singleton instance from `IServiceProvider`.

