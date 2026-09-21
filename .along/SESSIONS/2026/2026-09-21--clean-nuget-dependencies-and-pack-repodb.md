# Session: Clean NuGet Dependencies, Remove Ardalis.GuardClauses, and Pack ActDim.Practix.RepoDb

- **Date:** 2026-09-21
- **Agent:** antigravity
- **Issue:** [task--clean-nuget-dependencies-and-pack-repodb](../../ISSUES/done/task--clean-nuget-dependencies-and-pack-repodb.md)

## Goals
1. Configure `ActDim.Practix.RepoDb` as an independent packable NuGet package (`<IsPackable>true</IsPackable>`), eliminating unused internal project dependencies (`ActDim.Practix.Abstractions` and `ActDim.Practix.Common`).
2. Remove legacy `System.Runtime.Caching` from `ActDim.Practix.Common`.
3. Purge `Ardalis.GuardClauses` across all NuGet-published packages (`ActDim.Practix.RepoDb`, `ActDim.Practix.Common`, `ActDim.Reflectron`, `ActDim.Emitron`, `ActDim.Emitron.Razor`, `ActDim.Practix.Json`) and replace with standard .NET 10 BCL throw helpers (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentException.ThrowIfNullOrEmpty`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero`).
4. Purge unused package references from `ActDim.Practix.Service` and remove unneeded package versions from `Directory.Packages.props`.

## Changes Made
- **`ActDim.Practix.RepoDb`**:
  - Enabled `<IsPackable>true</IsPackable>`.
  - Removed `Ardalis.GuardClauses` dependency.
  - Removed `ProjectReference` to `ActDim.Practix.Abstractions` and `ActDim.Practix.Common`.
  - Verified standalone NuGet packaging producing clean nuspec with only RepoDb and Sqlite dependencies.
- **`ActDim.Practix.Common`**:
  - Removed `System.Runtime.Caching` package reference.
  - Removed `Ardalis.GuardClauses` package reference.
  - Deleted redundant `Extensions/GuardExtensions.cs`.
  - Migrated all validation in `AmbientContext`, `ConcurrentFactoryDictionary`, `WeakTable`, `StreamExtensions`, `EnumerableExtensions`, `DictionaryExtensions`, `FuncExtensions`, `LoggerExtensions`, `MethodBaseExtensions`, `CompressionManager`, `IntrospectionExtensions` to standard .NET 10 BCL throw helpers.
- **`ActDim.Reflectron`**:
  - Migrated all reflection accessors and dynamic invocations in `Reflectron.Properties.cs`, `Reflectron.Methods.cs`, `Reflectron.Fields.cs`, `Reflectron.Events.cs`, `Reflectron.Constructors.cs`, `Reflectron.Members.cs`, `Reflectron.Generic.cs`, `Extensions/TypeExtensions.cs`, and `Extensions/ObjectExtensions.cs` to standard .NET BCL throw helpers.
- **`ActDim.Emitron` & `ActDim.Emitron.Razor`**:
  - Removed `Ardalis.GuardClauses` package reference from both `.csproj` files.
  - Migrated `Emitron.cs`, `Interpolator.cs`, `RazorParser.cs`, `EmitronRazor.cs`, and `Extensions/StringExtensions.cs` to standard .NET BCL throw helpers.
- **`ActDim.Practix.Json` & `ActDim.Practix.Service`**:
  - Removed unused `Ardalis.GuardClauses` package references.
- **`Directory.Packages.props`**:
  - Removed `Ardalis.GuardClauses` and `System.Runtime.Caching` centrally.

## Verification
- Built full solution `ActDim.Practix.sln` with 0 errors.
- Ran full test suite across all projects in solution: 720 tests passed, 0 failed, 0 skipped.
- Packed `ActDim.Practix.RepoDb` and `ActDim.Practix.Common` into temporary packages and validated nuspec dependency graphs.
- Verified zero non-ASCII typographic characters across all modified files.
