---
protocol: along
slug: json-di-registration-missing-interfaces
type: bug
status: done
priority: high
created: 2026-09-15
updated: 2026-09-21
completed: 2026-09-21
agent: antigravity
tags: [json, di, dependency-injection, servicecollection, caching, pkg-practix-json]
milestone: v2.0.0-along-transition
blocked_by: []
related: [core-json-serializer-contract-and-merge-flaws]
---

# AddPractixJson DI Registration Missing Promised Interfaces

## Overview
`ServiceCollectionExtensions.AddPractixJson()` only registered `IJsonSerializer`. However, `CoreJsonSerializer` implements multiple serialization abstractions from `ActDim.Practix.Abstractions.Serialization`, and package documentation explicitly promises them. Downstream components such as `DistributedCachingProxy` depend on `IBinarySerializer` and fail when configured with standard DI setup.

## Identified Defects

### 1. Missing Interface Registrations in DI
In `Extensions/ServiceCollectionExtensions.cs`:
Only `IJsonSerializer` was added via `TryAddSingleton`.

### 2. Downstream Dependency Failure
In `ActDim.Practix.Common/Caching/DistributedCachingProxy.cs` (line 20):
`public DistributedCachingProxy(IDistributedCache cache, IBinarySerializer serializer)`
When consuming applications call `services.AddPractixJson()` as documented in `ActDim.Practix.Json/README.md`, resolving `IDistributedCachingProxy` or `IBinarySerializer` failed with `InvalidOperationException`.

## Remediation
1. Replaced redundant registration methods with `AddCoreJsonSerializer(this IServiceCollection services)`.
2. Registered shared singleton for `CoreJsonSerializer`, `IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, and `IStreamSerializer`.
3. Added unit tests in `Tests/Json.Tests/ServiceCollectionExtensionsTests.cs` verifying all 4 interfaces resolve to the same shared instance.

