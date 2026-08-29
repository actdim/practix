---
date: 2026-08-19
slug: ambient-context-enrichment-and-streamlining
agent: antigravity/gemini
branch: main
commit: pending
summary: Direct AsyncLocal storage in AmbientContext, removal of AmbientContextProvider, scoped services/user/cancellation/blobs/logging/compression API, BytePath abstractions relocation, solution-wide Nullable annotations, and documentation updates.
---

# 2026-08-19: Ambient Context Enrichment & Architecture Streamlining

## Context & Motivation
Following the removal of correlation IDs and obsolete control methods, the ambient execution context architecture was overhauled to eliminate unnecessary indirection (`AmbientContextProvider`), provide rich scoped access to `IServiceProvider`, `ClaimsPrincipal`, `CancellationToken`, `IBlobManager`, `ICompressionManager`, and fast zero-DI logging via `AmbientContext.Log<T>()`, relocate BytePath storage contracts to `ActDim.Practix.Abstractions`, and configure uniform `<Nullable>annotations</Nullable>` across all 24 projects in the solution.

## What Changed
1. **Elimination of `AmbientContextProvider` / `IAmbientContextProvider`:**
   - Deleted `IAmbientContextProvider.cs` and `AmbientContextProvider.cs`.
   - Embedded `AsyncLocal<ImmutableDictionary<string, object>>` directly in `AmbientContext`.
   - `AmbientContext.Current` returns the singleton `IAmbientContext` instance.
   - `services.AddAmbientContext()` registers `services.AddSingleton<IAmbientContext>(AmbientContext.Current)`.
   - Refactored `ActDim.Observability` (`ObservabilityContext`, `EventObservabilityLoggerFactory`, `EventObservabilityBridge`, and `EventObservabilityExtensions`) to consume `IAmbientContext` directly.

2. **Scoped Ambient Execution Context API & Extensions:**
   - Moved `AmbientKeys.cs` and BytePath storage contracts (`IBlobManager.cs`, `IBlobDataStore.cs`, `IBlobRegistry.cs`, `BlobRecord.cs`, `BlobResult.cs`, `BlobStoreOptions.cs`, `BlobErrorCode.cs`, `LockType.cs`, `ProducerStreamBridge.cs`) to `ActDim.Practix.Abstractions`.
   - Created `AmbientContextExtensions.cs` on `IAmbientContext` providing `GetServices()`/`WithServices()`, `GetUser()`/`WithUser()`, `GetCancellationToken()`/`WithCancellationToken()`, `GetBlobManager()`/`WithBlobManager()`, `GetLoggerFactory()`/`WithLoggerFactory()`, and `GetCompressionManager()`/`WithCompressionManager()`.
   - Streamlined static `AmbientContext` facade with clean 1-line delegates to `Current.*`:
     - `AmbientContext.Services` (throws `InvalidOperationException` if no scope) & `AmbientContext.WithServices(sp)`
     - `AmbientContext.User` & `AmbientContext.WithUser(user)`
     - `AmbientContext.CancellationToken`, `AmbientContext.WithCancellationToken(ct)`, & `AmbientContext.WithTimeout(timeout, out token)`
     - `AmbientContext.Blobs` & `AmbientContext.WithBlobManager(bm)`
     - `AmbientContext.Compression` & `AmbientContext.WithCompressionManager(cm)`
     - `AmbientContext.LoggerFactory`, `AmbientContext.WithLoggerFactory(lf)`, & `AmbientContext.Log<T>()` / `AmbientContext.Log(type)` / `AmbientContext.Log(instance)`

3. **Solution-Wide `<Nullable>annotations</Nullable>` Adoption:**
   - Updated all 24 `.csproj` files to `<Nullable>annotations</Nullable>` allowing `?` type annotations without compiler noise or warning overhead.
   - Removed redundant `#nullable enable` directives across all 22 `.cs` files.

4. **Testing & Quality Assurance:**
   - Reorganized test suite into `AmbientContextTests.cs` (Unit) and `AmbientContextHostingTests.cs` (Host/Server Integration):
     - Scoped `WithServices`, `WithUser`, `WithCancellationToken`, `WithTimeout`, `WithBlobManager`, `WithCompressionManager`, `WithLoggerFactory`, and `Log<T>()`.
     - Direct `IAmbientContextExtensions` usage on interface instances.
     - `WithCancellationToken_CombinesWithExistingAmbientToken_UsingLinkedTokenSource` verifying chained token cancellation.
     - `GenericHost_RootAmbientContext_FlowsToHostedBackgroundService` verifying background worker execution.
     - `WebApplication_RootContextWithScopedRequestMiddleware_DemonstratesFullLifecycle` verifying root and per-request middleware isolation.
   - All 551 tests across `ActDim.Practix.sln` (and 586 across entire workspace) pass with 0 failures and 0 warnings.

5. **Documentation Updates:**
   - Updated `ActDim.Practix.Common/README.md` with complete developer guides, examples for Console/Worker and Web applications, and updated metrics.
   - Updated root `README.md` and `ActDim.Observability/README.md` test counts.

## Files Touched
- `ActDim.Practix.Abstractions/Context/IAmbientContextProvider.cs` (DELETED)
- `ActDim.Practix.Common/Context/AmbientContextProvider.cs` (DELETED)
- `ActDim.Practix.Abstractions/Context/AmbientKeys.cs` (CREATED)
- `ActDim.Practix.Abstractions/Context/AmbientContextExtensions.cs` (CREATED)
- `ActDim.Practix.Abstractions/Storage/*` (CREATED / MOVED from BytePath)
- `ActDim.Practix.Common/Context/AmbientContext.cs` (MODIFIED)
- `ActDim.Practix.Common/Extensions/ServiceCollectionExtensions.cs` (MODIFIED)
- `ActDim.Observability/ObservabilityContext.cs` (MODIFIED)
- `ActDim.Observability/EventObservabilityLoggerFactory.cs` (MODIFIED)
- `ActDim.Observability/EventObservabilityBridge.cs` (MODIFIED)
- `ActDim.Observability/Extensions/EventObservabilityExtensions.cs` (MODIFIED)
- `Tests/Common.Tests/Context/AmbientContextTests.cs` (MODIFIED)
- `Tests/Common.Tests/Context/AmbientContextHostingTests.cs` (CREATED)
- `Tests/Common.Tests/Context/AmbientContextEnrichmentTests.cs` (DELETED)
- `ActDim.Practix.Common/README.md` (MODIFIED)
- `README.md` (MODIFIED)
- `.agents/DECISIONS.md` (MODIFIED)
- All 24 `*.csproj` files (MODIFIED)

## Decisions
- ADR-015: Direct AsyncLocal Storage in `AmbientContext` and Elimination of `AmbientContextProvider`.
- ADR-016: Scoped Ambient Execution Primitives, Centralized `AmbientKeys`, and Solution-Wide Nullable Annotations.
