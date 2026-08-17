---
slug: debt--remove-autofac-dependency
type: debt
status: done
priority: high
created: 2026-08-17
updated: 2026-08-17
---

# Remove Autofac dependency and migrate to standard Microsoft Dependency Injection

## Context
The solution used Autofac modules (`TrackableModule`, `CommonModule`, `JsonModule`, `ServiceModule`, `AutofacServiceProviderFactory`). To standardize on the .NET BCL DI container (`Microsoft.Extensions.DependencyInjection`), Autofac was completely removed from all projects.

## Objectives
- Deleted `TrackableModule.cs` wrapper and Autofac `Module` classes.
- Removed `Autofac` and `Autofac.Extensions.DependencyInjection` NuGet packages from all project files.
- Created standard `IServiceCollection` extension methods (`AddPractixCommon()`, `AddPractixJson()`, `AddPractixService()`).
- Refactored `CoreService` to use standard `IServiceCollection` and `BuildServiceProvider()`.
- Verified 100% solution test suite passing (467/467 tests passed).
