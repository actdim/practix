---
protocol: along
date: 2026-09-09
slug: bugfixes-and-version-bump-1-0-15
agent: antigravity
branch: main
commit: pending
summary: Fixed concurrency bugs, lock leaks, sync-over-async calls, updated test suites, and bumped version to 1.0.15
milestone: v2.0.0-along-transition
issues_advanced: [feat--unified-configurable-authentication]
issues_completed: [bug--async-object-pool-disposal-race, bug--fix-using-statement-false-positive, bug--fix-lock-leak-on-io-failure, bug--fix-sync-over-async-constructor, bug--fix-sync-over-async-runner, bug--fix-service-tests, bug--fix-app-registry-service]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Bugfixes, Concurrency Hardening, and Version 1.0.15 Bump

## Overview
Comprehensive hardening session addressing concurrency race conditions, lock leaks, sync-over-async anti-patterns, test runner failures, and version bump across the Practix .NET solution.

## Accomplishments
1. **`ActDim.Practix.Common`**:
   - Resolved race condition in `AsyncObjectPool<T>` when `ReturnAsync` or `DiscardAsync` race against `DisposeAsync()`.
   - Added post-semaphore draining and exception suppression on disposed semaphores.
   - Added `DisposeAsync_WithConcurrentReturns_CleansUpAllItemsWithoutException`.
2. **`ActDim.Emitron`**:
   - Fixed `ScriptInternals.FindInjectionIndex` false positives on `using (...)` statements vs namespace `using` directives.
   - Fixed sync-over-async in `Emitron.cs`: cached compiled delegates via `script.CreateDelegate()`, protected against single-thread `SynchronizationContext` deadlocks, and added async APIs (`CompileAsync`, `EvaluateAsync`).
3. **`ActDim.BytePath`**:
   - Fixed write lock leak in `BlobManager.cs` when `DataStore.GetSizeAsync` throws during `DisposeAsync`.
   - Guaranteed lock cleanup in `finally` blocks.
4. **`ActDim.BytePath.SqliteRegistry`**:
   - Eliminated sync-over-async constructor call `EnsureSchemaAsync().GetAwaiter().GetResult()`.
   - Replaced with synchronous `EnsureSchema()` executing native SQLite schema commands directly.
5. **`ActDim.Practix.Service` & `AppRegistry`**:
   - Fixed test project reference and compilation in `ActDim.Practix.Service.Tests.csproj`.
   - Fixed filename typo with trailing space in `AppRegistryService.cs`.
   - Scaffolded configurable multi-scheme authentication issues for Zitadel OIDC and multi-scheme support.
6. **Solution Quality**:
   - Bumped version in `Directory.Build.props` to `1.0.15`.
   - All 660 automated tests pass with 0 failures across all 9 test assemblies.
