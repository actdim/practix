---
slug: debt--remove-autofac-dependency
type: debt
status: in-progress
priority: high
created: 2026-08-17
updated: 2026-08-17
---

# Remove Autofac dependency and migrate to standard Microsoft Dependency Injection

## Context
The solution used Autofac modules (`TrackableModule`, `CommonModule`, `JsonModule`, `ServiceModule`, `AutofacServiceProviderFactory`). To standardize on the .NET BCL DI container (`Microsoft.Extensions.DependencyInjection`), Autofac is being removed from all projects.

## Objectives
- Delete `TrackableModule.cs` and Autofac `Module` classes.
- Remove `Autofac` and `Autofac.Extensions.DependencyInjection` NuGet packages from all project files.
- Create standard `IServiceCollection` extension methods (`AddPractixCommon()`, `AddPractixJson()`, `AddPractixService()`, etc.).
- Refactor `CoreService` to use standard `IServiceCollection` and `BuildServiceProvider()`.
- Verify 100% solution test suite passing.
