---
protocol: along
date: 2026-08-17
slug: remove-autofac-dependency
agent: antigravity
branch: main
commit: head
summary: Removed Autofac completely, created fine-grained Microsoft DI extensions (AddAmbientContext, AddCompressionManager, AddMemoryCachingProxy, AddDistributedCachingProxy), and renamed AddPractixService to AddCoreService.
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session Log: Remove Autofac Dependency & Granular Microsoft DI Extensions

## Changes Made & Rationale
- **Removed Autofac Packages & Modules**:
  - Deleted `TrackableModule.cs` wrapper and all Autofac `Module` classes.
  - Removed `Autofac` and `Autofac.Extensions.DependencyInjection` NuGet packages from all project files.
- **Fine-Grained `IServiceCollection` Extension Methods**:
  - In `ActDim.Practix.Common.Extensions`: Replaced `AddPractixCommon` with granular methods:
    - `AddAmbientContext()`
    - `AddCompressionManager()`
    - `AddMemoryCachingProxy()`
    - `AddDistributedCachingProxy()`
- **Renamed Service Registration**:
  - Renamed `AddPractixService()` to `AddCoreService()` in `ActDim.Practix.Service.Extensions`.
  - `AddCoreService()` chains `AddAmbientContext()`, `AddCompressionManager()`, `AddMemoryCachingProxy()`, `AddDistributedCachingProxy()`, and `AddPractixJson()`.
- **Refactored `CoreService`**:
  - Replaced `AutofacServiceProviderFactory` with standard BCL DI (`services.AddCoreService(); services.BuildServiceProvider();`).

## Files Touched
- `ActDim.Practix.Common/Extensions/ServiceCollectionExtensions.cs`
- `ActDim.Practix.Service/Extensions/ServiceCollectionExtensions.cs`
- `ActDim.Practix.Service/CoreService.cs`

## Verification
- Executed `dotnet test ActDim.Practix.sln`.
- All 467 tests across 5 test assemblies passed cleanly (0 failures, 0 errors).
