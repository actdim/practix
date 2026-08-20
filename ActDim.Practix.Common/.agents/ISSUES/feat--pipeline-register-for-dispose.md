---
slug: pipeline-register-for-dispose
type: feat
status: open
priority: medium
created: 2026-08-20
updated: 2026-08-20
---

# Feature: Pipeline & Execution Scope End-of-Life Resource Registration (`RegisterForDispose`)

## Goal
Provide a decoupled mechanism (via ASP.NET Core request pipeline middleware, `ActDim.Practix.Service`, and execution scopes) to register arbitrary `IDisposable` and `IAsyncDisposable` objects created deep within business logic for automatic cleanup at the end of an HTTP request or background execution scope.

## Problem Statement & Context
1. **The Cleanup Gap Beyond Streams**:
   - While ASP.NET Core automatically disposes `Stream` instances returned via `FileStreamResult`, arbitrary `IDisposable` / `IAsyncDisposable` objects created deep within service layers (e.g. temporary unmanaged buffers, opened archive handles, temporary files, custom connectors) are NOT automatically tracked by the Web framework unless explicitly registered.
2. **Coupling to `HttpContext`**:
   - Registering resources directly via `HttpContext.Response.RegisterForDispose(obj)` forces deep application services to depend on `IHttpContextAccessor` or `HttpContext`, violating architectural decoupling.
   - Non-Web environments (background hosted services, queue workers, console apps) lack `HttpContext` entirely.

## Architectural Options & Flexible Solutions
1. **ASP.NET Core Pipeline Integration (`ActDim.Practix.Service`)**:
   - Leverage ASP.NET Core middleware / action filters / request scope services to automatically capture and dispose request-scoped resources.

2. **Decoupled Registration API**:
   - Provide a framework facade (e.g. `RegisterForDispose` in execution pipeline or context) so business code can register disposables without referencing ASP.NET Core web abstractions.
   - In ASP.NET Core, automatically forward registered disposables to `HttpContext.Response.RegisterForDispose(...)`.

3. **Standalone / Background Execution Scopes (`CreateExecutionScope`)**:
   - Provide scoped execution scopes for background workers, queue consumers, and console applications to clean up all registered resources upon scope completion.

4. **Unit & Integration Tests**:
   - Verify proper disposal order, error handling during disposal, thread-safe registration, and ASP.NET Core pipeline bridging.
