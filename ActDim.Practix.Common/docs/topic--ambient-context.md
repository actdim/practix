---
protocol: along
protocol_version: "2.2.18"
slug: ambient-context
title: Ambient Execution Context
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [context, ambient, async-local, logging, dependency-injection]
---

# Ambient Execution Context

`AmbientContext` provides asynchronous execution flow context management, allowing scoped services, user identities, cancellation tokens, blob managers, and custom execution state to propagate down the asynchronous call hierarchy (`async`/`await`, `Task.Run`, background tasks) without manually threading parameters through every method signature.

---

## Core Architecture & Thread-Safety

Unlike global static state (which causes race conditions when accessed concurrently across threads), `AmbientContext` is backed by `AsyncLocal<ImmutableDictionary<string, object>>`:

- **Immutable State Transitions**: State updates produce new immutable dictionaries bound to the ambient `ExecutionContext`.
- **Downstream Flow**: Child asynchronous tasks inherit the parent context state at the time of invocation.
- **Scope Isolation**: Temporary overrides via `using (AmbientContext.With...)` apply exclusively to the current execution branch and restore the previous state upon scope disposal.

```
Caller Scope [Services=A, User=Admin]
    |
    +---> Task 1 (inherits Services=A, User=Admin)
    |        |
    |        +---> using(AmbientContext.WithUser(UserX)) --> Child Scope [User=UserX]
    |
    +---> Task 2 (isolated: still has Services=A, User=Admin)
```

---

## Scoped Overrides & Lifetime Scopes

All scoped mutators return an `IDisposable` (implemented via `DisposableAction<T>`) that pops or reverts the specific ambient key upon disposal:

```csharp
// Scope with custom services
using (AmbientContext.WithServices(serviceProvider))
{
    var service = AmbientContext.Services.GetRequiredService<IMyService>();
}

// Scope with specific claims principal
using (AmbientContext.WithUser(claimsPrincipal))
{
    var currentUser = AmbientContext.User;
}

// Scope with combined cancellation token
using (AmbientContext.WithCancellationToken(cancellationToken))
{
    await DoWorkAsync(AmbientContext.CancellationToken);
}

// Scope with temporary timeout
using (AmbientContext.WithTimeout(TimeSpan.FromSeconds(5), out var timeoutToken))
{
    // AmbientContext.CancellationToken is automatically linked to the 5-second deadline
    await httpClient.GetAsync("/api/data", AmbientContext.CancellationToken);
}
```

---

## ASP.NET Core Middleware Integration

In web applications, ambient context can be established at the HTTP request boundary to bind request-scoped dependencies:

```csharp
app.Use(async (context, next) =>
{
    using var _s = AmbientContext.WithServices(context.RequestServices);
    using var _u = AmbientContext.WithUser(context.User);
    using var _c = AmbientContext.WithCancellationToken(context.RequestAborted);
    using var _t = AmbientContext.Push("TraceId", context.TraceIdentifier);

    await next();
});
```

---

## Zero-DI Logging & Structured Method Scopes

`AmbientContext.Log<T>()` and `AmbientContext.Log(this)` resolve `ILogger<T>` on demand using the ambient `IServiceProvider` or `ILoggerFactory` without constructor injection ceremony.

`BeginMethodScope()` enriches logs with OpenTelemetry semantic conventions (`code.function`, `code.filename`, `code.filepath`, `code.lineno`):

```csharp
public class OrderProcessor
{
    public void Process(string orderId)
    {
        using var scope = AmbientContext.Log<OrderProcessor>().BeginMethodScope();

        AmbientContext.Log<OrderProcessor>().LogInformation("Processing order {OrderId}", orderId);
        AmbientContext.Log(this).LogInformation("Order completed");
    }
}
```

---

## Key Invariants

1. **Explicit Disposal**: Always dispose scope handles (`using`) to prevent ambient state leakage across async continuations in pooled worker threads.
2. **Fallback Safety**: If no ambient service provider or logger factory is registered, `AmbientContext.Log` falls back to `NullLogger` without throwing null reference exceptions.
3. **No Thread-Static Mutation**: Does not mutate thread-local storage or thread statics; execution flow is entirely governed by .NET `AsyncLocal<T>`.

