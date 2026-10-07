---
protocol: along
slug: ambient-context
title: Ambient Execution Context
type: topic
created: 2026-09-03
updated: 2026-09-22
tags: [context, ambient, async-local, flow-state, dependency-injection, architecture]
---

# Ambient Execution Context

`AmbientContext` is an asynchronous execution flow state engine for .NET. It allows contextual execution state (such as cancellation tokens, user identities, scoped service providers, and ambient infrastructure handles) to flow naturally down the asynchronous call hierarchy (`async`/`await`, `Task.Run`, background tasks) without manually drilling parameters through every method signature in business logic.

---

## 1. Motivation: Pragmatic Flow vs Textbook Dogmatism

In modern enterprise and cloud software development, adhering strictly to textbook dogma often introduces substantial architectural friction:

### The "Parameter Drilling" Problem
In complex business logic, operations frequently span 5 to 15 layers of services, handlers, domain entities, and repositories. When following the rule that every single method must explicitly take parameters for `CancellationToken`, `ClaimsPrincipal`, `CorrelationId`, and `TenantId`:
- **Signature Churn**: Adding cancellation or tenant support to a low-level method requires modifying every intermediate method signature in the call chain.
- **Contract Pollution**: Pure business contracts become cluttered with infrastructural plumbing parameters.
- **Human Error**: Developers often forget to forward `CancellationToken` in intermediate helper calls, silently breaking cooperative cancellation.

### The "Constructor Bloat" Problem
In business orchestration services, a single handler may need to coordinate multiple infrastructural tasks: logging, auditing, tenant validation, temporary blob storage, and compression. Injecting 8 to 12 dependencies into a single constructor just to pass them down creates bloated, fragile classes that are difficult to instantiate and maintain.
In business orchestration services, a single handler may need to coordinate multiple infrastructural tasks: logging, auditing, tenant validation, temporary blob storage, and compression. Injecting 8 to 12 dependencies into a single constructor just to pass them down creates bloated, fragile classes that are difficult to instantiate, mock, and maintain.

`AmbientContext` solves both problems by establishing an **ambient execution boundary** at the entry point of a logical flow (e.g. HTTP middleware, message consumer, or background service) and allowing downstream business code to access context seamlessly.

---

## 2. Addressing Criticisms: The Skeptic's FAQ
## 2. Deep Dive: AsyncLocal Mechanics & Complete Sibling Isolation

When senior engineers first encounter `AmbientContext`, they frequently raise valid concerns based on past experiences with bad architectures. Here is how `AmbientContext` addresses each concern:
The most common misconception from engineers who haven't worked with modern .NET runtime internals is confusing `AmbientContext` with a "global mutable state" or a static singleton dictionary with locks.

### "Isn't this global mutable state (a God Object)?"
**No.** `AmbientContext` does not use global mutable state or thread-static variables.
- It is backed by `AsyncLocal<ImmutableDictionary<string, object>>`.
- State updates are **copy-on-write**: creating a scope generates a new immutable dictionary branch bound exclusively to the current asynchronous execution flow (`ExecutionContext`).
- Sibling tasks and parent flows are completely isolated: a modification in Task A cannot leak into Task B.
- Upon disposing the scope (`using`), the previous state is deterministically restored.
### Why It Is NOT Global State
`AmbientContext` is backed by `AsyncLocal<ImmutableDictionary<string, object>>` and .NET `ExecutionContext`:

1. **Logical Execution Inheritance**: When a parent asynchronous flow spawns child tasks (e.g. via `Task.Run`, `Task.WhenAll`, or `Parallel.ForEachAsync`), each child task automatically inherits a snapshot of the parent's ambient context at the moment of invocation.
2. **Copy-on-Write Branching**: Any state modification (such as `PushProperty`, `WithUser`, or `WithServices`) produces a **new immutable dictionary branch** that is assigned exclusively to the current asynchronous execution branch.
3. **Strict Sibling Isolation (Zero Cross-Task Pollution)**:
   If a parent flow spawns 3 concurrent tasks, and Task 1 overrides a property, **it cannot affect Task 2, Task 3, or the parent flow**. Sibling tasks are completely isolated.

```
Parent Flow [FlowId="Main", Tenant="T1"]
    |
    +---> Task 1 (inherits [FlowId="Main", Tenant="T1"])
    |        |
    |        +---> using(AmbientContext.Current.PushProperty("FlowId", "Branch_1"))
    |                 Scoped Context: [FlowId="Branch_1", Tenant="T1"]
    |                 (CANNOT mutate Task 2, Task 3, or Parent)
    |
    +---> Task 2 [FlowId="Main", Tenant="T1"]  <-- COMPLETELY ISOLATED
    |
    +---> Task 3 [FlowId="Main", Tenant="T1"]  <-- COMPLETELY ISOLATED
```

### Deterministic Lifetime (RAII)
Every ambient mutator returns an `IDisposable` scope handle:
```csharp
using (AmbientContext.Current.WithUser(specialUser))
{
    // Execution branch has User = specialUser
    await ProcessTaskAsync();
}
// Exiting using deterministically restores the previous user (or removes it)
```
Disposing the scope handle restores the prior immutable dictionary reference. There is no shared mutable memory, no lock contention, and no risk of concurrent modification exceptions.

---

## 3. Addressing Criticisms: The Skeptic's FAQ

### "Isn't this the Service Locator anti-pattern?"
**No.** A Service Locator replaces Dependency Injection entirely and turns classes into opaque dependency consumers.
- In `AmbientContext`, **Dependency Injection remains the primary mechanism** for composing the system. Singletons, scoped services, and factories are configured in `IServiceCollection` as usual.
- `AmbientContext` simply acts as a **runtime propagation bridge**: it carries the active `IServiceProvider` and execution flow metadata down the asynchronous call tree.
- The core interface `IAmbientContext` does not expose a monolithic list of services; it only exposes `Properties` and `PushProperty`. All typed accessors are modular extension methods.
**No.** A Service Locator replaces Dependency Injection entirely, turning classes into opaque dependency consumers that cannot declare what they need.
- In this architecture, **Dependency Injection remains the primary mechanism** for composing the system. Singletons, scoped services, and factories are configured in `IServiceCollection` as usual.
- `AmbientContext` does not compete with DI. It acts as a **runtime execution bridge**: it carries the active scoped `IServiceProvider` down the asynchronous call tree so that business orchestration code can resolve scoped services when needed.
- The core interface `IAmbientContext` does not expose a monolithic list of services; it only exposes `Properties` and `PushProperty`.

### "Why not pass CancellationToken explicitly everywhere?"
- Explicit parameter passing is recommended for **low-level utility libraries** (e.g. stream readers, socket handlers, serialization engines) where methods are pure and contracts are self-contained.
- However, in **high-level business pipelines**, cancellation is an execution flow attribute, not an algorithm parameter. Ambient propagation ensures that cancellation is always active, even in methods that were written without explicit token parameters.
- Explicit parameter passing is recommended for **low-level utility libraries** (e.g. stream readers, socket handlers, serialization engines) where methods are pure, self-contained algorithms.
- In **high-level business pipelines and orchestration**, cancellation is an execution flow attribute, not an algorithm input. Ambient propagation ensures that cancellation is honored across the entire operation, even through legacy or intermediate methods that lacked explicit token parameters.

---

## 3. Boundary of Applicability: Where to Use vs Where NOT to Use
## 4. Boundary of Applicability: Where to Use vs Where NOT to Use

To maintain clean architecture, developers must respect the boundary of where `AmbientContext` belongs:

| Layer / Scenario | Recommended Approach | Rationale |
| :--- | :--- | :--- |
| **Application Boundaries** (HTTP Middleware, gRPC Interceptors, Queue Consumers) | **Establish `AmbientContext`** | Sets up the execution environment (`Services`, `User`, `CancellationToken`, `TraceId`). |
| **Business Logic Orchestration** (Use cases, application services, domain workflows) | **Read `AmbientContext`** | Avoids parameter drilling and constructor bloat across deep call trees. |
| **Core Infrastructure Services** (Blob storage, compression, caching) | **Use Constructor DI** | Infrastructure components should declare explicit dependencies in their constructors. |
| **Pure Utilities & Algorithms** (Math, parsers, codecs, collection helpers) | **Explicit Parameters Only** | Utility functions must remain pure, deterministic, and free of ambient dependencies. |

---

## 4. Architecture: Minimal Core & Modular Extension Namespaces
## 5. Architecture: Minimal Core, Modular Extensions & Custom Wrappers

The core abstraction `IAmbientContext` is completely minimal and unpolluted:
### Pristine Core Abstraction
The core abstraction `IAmbientContext` is completely minimal and unpolluted by domain or infrastructure dependencies:

```csharp
namespace ActDim.Practix.Abstractions.Context
{
    public interface IAmbientContext
    {
        IReadOnlyDictionary<string, object> Properties { get; }
        IDisposable PushProperty(string name, object value);
    }
}
```

### Modular, Opt-In Extension Namespaces
All typed accessors are separated into **modular extension classes** located in the namespaces of their respective contracts. They only become visible in IntelliSense when the corresponding namespace is imported:

| Capability | Extension Class | Namespace to Import | Methods Provided |
| :--- | :--- | :--- | :--- |
| **Flow Context** | `AmbientContextExtensions` | `ActDim.Practix.Abstractions.Context.Extensions` | `GetServices()`, `WithServices()`, `GetUser()`, `WithUser()`, `GetCancellationToken()`, `WithCancellationToken()`, `WithTimeout()` |
| **Storage** | `AmbientContextBlobExtensions` | `ActDim.Practix.Abstractions.Storage` | `GetBlobManager()`, `WithBlobManager()` |
| **Compression** | `AmbientContextCompressionExtensions` | `ActDim.Practix.Abstractions.Compression` | `GetCompressionManager()`, `WithCompressionManager()` |
| **Logging** | `AmbientContextLoggingExtensions` | `ActDim.Practix.Abstractions.Logging` | `GetLoggerFactory()`, `WithLoggerFactory()` |
| **Memory** | `AmbientContextMemoryExtensions` | `ActDim.Practix.Abstractions.Memory` | `GetMemoryManager()`, `WithMemoryManager()` |

### Custom Subsystem Wrappers & Facades
Because `IAmbientContext` is a clean property bag, any application, subsystem, or feature team can create their own custom strongly-typed wrappers or facades without modifying the core library or polluting other teams' IntelliSense:

```csharp
// Example: Team creating a strongly-typed tenant scope
public static class TenantContextExtensions
{
    private const string TenantKey = "MyApp.TenantId";

    public static string? GetTenantId(this IAmbientContext context) =>
        context.Properties.TryGetValue(TenantKey, out var val) ? val as string : null;

    public static IDisposable WithTenantId(this IAmbientContext context, string tenantId) =>
        context.PushProperty(TenantKey, tenantId);
}

// Or an object-oriented wrapper class:
public sealed class OrderExecutionContext(IAmbientContext ambient)
{
    public string OrderId => ambient.Properties["OrderId"]?.ToString() ?? string.Empty;
    public string? TenantId => ambient.GetTenantId();
    public CancellationToken CancellationToken => ambient.GetCancellationToken() ?? CancellationToken.None;
}
```

This ensures complete flexibility: teams can choose extension methods, dedicated wrapper classes, or raw property bag access according to their project conventions.

---

## 5. Production Integration Patterns
## 6. Production Integration Patterns

### Pattern A: ASP.NET Core Middleware Integration
Establish the ambient context at the HTTP request boundary so all endpoints and downstream services share the request scope:

```csharp
app.Use(async (context, next) =>
{
    using var _s = AmbientContext.Current.WithServices(context.RequestServices);
    using var _u = AmbientContext.Current.WithUser(context.User);
    using var _c = AmbientContext.Current.WithCancellationToken(context.RequestAborted);
    using var _t = AmbientContext.Current.PushProperty("RequestId", context.TraceIdentifier);

    await next();
});

app.MapGet("/api/orders", () =>
{
    // Business code accesses active request services and tokens directly
    var orderService = AmbientContext.Current.GetServices()!.GetRequiredService<IOrderService>();
    var user = AmbientContext.Current.GetUser();
    var ct = AmbientContext.Current.GetCancellationToken() ?? CancellationToken.None;

    return orderService.GetOrdersForUserAsync(user, ct);
});
```

### Pattern B: Generic Host / Background Worker Integration
Bind the host-level service provider and shutdown cancellation token to background workers:

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddHostedService<QueueProcessingWorker>();
            })
            .Build();

        using var appCts = new CancellationTokenSource();

        // Establish root ambient context for the entire application lifetime
        using (AmbientContext.Current.WithServices(host.Services))
        using (AmbientContext.Current.WithCancellationToken(appCts.Token))
        {
            await host.RunAsync();
        }
    }
}

public class QueueProcessingWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Inherits host services from ambient context without constructor injection
        var services = AmbientContext.Current.GetServices()!;
        var processor = services.GetRequiredService<IMessageProcessor>();

        await processor.ProcessAsync(stoppingToken);
    }
}
```

### Pattern C: Linked Timeout Scope (`WithTimeout`)
Apply a temporary deadline to an async operation that automatically links with the parent cancellation token:

```csharp
// Applies a 5-second deadline linked to the ambient CancellationToken
using (AmbientContext.Current.WithTimeout(TimeSpan.FromSeconds(5), out var timeoutToken))
{
    // If the parent token cancels OR 5 seconds elapse, timeoutToken cancels
    await httpClient.GetAsync("https://api.external.com/data", timeoutToken);
}
// Exiting the scope restores the un-timed parent cancellation token
```

### Pattern D: Hierarchical Scope Overrides
Temporarily impersonate a user or override a tenant for a specific block of code:

```csharp
// Outer context has User = Alice
using (AmbientContext.Current.WithUser(adminUser))
{
    // Inner context has User = Admin
    await ExecutePrivilegedTaskAsync();
}
// Automatically reverts to User = Alice
```

---

## 6. Summary: Key Rules for the Team
## 7. Summary: Key Rules for the Team

1. **Always use `using` with scopes**: Every mutator (`WithServices`, `WithUser`, `WithCancellationToken`, `WithTimeout`, `WithBlobManager`) returns an `IDisposable`. Always use `using` or `using var` to guarantee deterministic cleanup.
2. **Do not use in low-level utility libraries**: Keep math, parsers, and serialization libraries pure with explicit parameters.
3. **Import only what you need**: Add `using ActDim.Practix.Abstractions.Context.Extensions;` only where flow state access is genuinely required.
4. **Extend freely**: Create domain-specific extensions or wrappers over `IAmbientContext` to match your application's ubiquitous language.
