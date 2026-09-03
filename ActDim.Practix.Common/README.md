# ActDim.Practix.Common

`ActDim.Practix.Common` provides shared ambient execution context primitives, concurrent collection factories, caching proxies, compression helpers, memory buffer management, and Microsoft Dependency Injection extensions for the ActDim.Practix framework.

## Features

- **Ambient Execution Context:** High-performance, zero-allocation ambient context management via `AmbientContext` and `IAmbientContext` (`AsyncLocal<ImmutableDictionary<string, object>>`).
- **Scoped Service & Dependency Resolution:** Access scoped `IServiceProvider`, `ClaimsPrincipal` user, `CancellationToken`, `IBlobManager`, and `ICompressionManager` from anywhere without manual constructor parameter passing.
- **Fast Zero-DI Logging:** `AmbientContext.Log<T>()` and `AmbientContext.Log(this)` resolving ambient/scoped logger factories with zero ceremony.
- **Caching Proxies:** `MemoryCachingProxy` and `DistributedCachingProxy` offering simplified, resilient access to `IMemoryCache` and `IDistributedCache`.
- **Stream & Payload Compression:** `CompressionManager` for `GZip`, `BZip2`, and `ZLib` buffer/stream compression.
- **High-Performance Collections:** `ConcurrentFactoryDictionary` (thread-safe lock-free lookup/factory pattern), `WeakTable<K, V>`, and `CompositeKey`.
- **Memory Buffer Pooling:** Zero-allocation buffer management with `ArrayPoolBufferOwner` and `MemoryManager`.
- **Granular Microsoft DI Extensions:** Modular registration helpers (`AddAmbientContext()`, `AddCompressionManager()`, `AddMemoryCachingProxy()`, `AddDistributedCachingProxy()`).

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Practix.Common
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Practix.Common
```

## Dependency Injection Setup

Register components with Microsoft Dependency Injection (`IServiceCollection`):

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Register ambient context singleton
    services.AddAmbientContext();

    // Register compression manager
    services.AddCompressionManager();

    // Register memory and distributed caching proxies
    services.AddMemoryCachingProxy();
    services.AddDistributedCachingProxy();
}
```

---

## Ambient Context Usage Guide

`AmbientContext` allows passing execution state (services, user identity, cancellation tokens, blob managers, memory manager, custom metadata) down the asynchronous execution tree (`async/await`, `Task.Run`, background workers) without threading parameters through every method signature.

> [!NOTE]
> **Thread Safety & Execution Flow Isolation:**  
> Unlike global static variables (which mutate global state and cause race conditions when accessed concurrently across threads), `AmbientContext` is backed by `AsyncLocal<ImmutableDictionary<string, object>>`. Context mutations flow strictly down the async execution tree (`async/await`, `Task.Run`). Temporary `using (AmbientContext.With...)` overrides apply exclusively to the current call branch without cross-thread pollution or race conditions.

### 1. Console / Worker Application (`Program.cs`)

Wrap the host lifetime in `AmbientContext.WithServices` so all background services and workers automatically inherit root services and cancellation tokens:

```csharp
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddAmbientContext();
        services.AddSingleton<IOrderProcessingService, OrderProcessingService>();
        services.AddHostedService<QueueWorker>();
    })
    .Build();

using var appCts = new CancellationTokenSource();

// ══ Application Root Scope ═════════════════════════════════════════════════
using (AmbientContext.WithServices(host.Services))
using (AmbientContext.WithCancellationToken(appCts.Token))
{
    await host.RunAsync();
}

// Background Worker automatically inherits AmbientContext
public class QueueWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Zero-DI logging
        var logger = AmbientContext.Log<QueueWorker>();
        logger.LogInformation("Worker started");

        // Resolve dependencies directly from ambient context
        var processor = AmbientContext.Services.GetRequiredService<IOrderProcessingService>();
        await processor.ProcessNextBatchAsync(AmbientContext.CancellationToken);
    }
}
```

---

### 2. ASP.NET Core Web Application & Request Middleware

In web applications, establish the root context for the application, and use a lightweight middleware to establish scoped per-request overrides (`RequestServices`, `User`, `RequestAborted`):

```csharp
using ActDim.Practix.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAmbientContext();
builder.Services.AddScoped<IOrderService, OrderService>();

await using var app = builder.Build();

// ══ 1. Per-Request Ambient Middleware ══════════════════════════════════════
app.Use(async (context, next) =>
{
    using var _s = AmbientContext.WithServices(context.RequestServices);
    using var _u = AmbientContext.WithUser(context.User);
    using var _c = AmbientContext.WithCancellationToken(context.RequestAborted);
    using var _t = AmbientContext.Push("TraceId", context.TraceIdentifier);

    await next();
});

// ══ 2. Endpoint Handler / Business Logic ═══════════════════════════════════
app.MapGet("/orders/current", () =>
{
    // Resolves scoped service for the current HTTP request
    var orderService = AmbientContext.Services.GetRequiredService<IOrderService>();
    var currentUser = AmbientContext.User;
    var cancellationToken = AmbientContext.CancellationToken;

    var order = orderService.GetOrderForUser(currentUser.Identity?.Name, cancellationToken);
    return Results.Ok(order);
});

// ══ 3. Root Application Scope ══════════════════════════════════════════════
using var _rootServices = AmbientContext.WithServices(app.Services);
using var _rootCt = AmbientContext.WithCancellationToken(app.Lifetime.ApplicationStopping);

await app.RunAsync();
```

---

### 3. Combining Cancellation Tokens & Scoped Timeouts

You can link nested cancellation tokens or apply scoped timeouts without modifying method signatures:

```csharp
// Link existing ambient token with a child timeout token
using var timeoutScope = AmbientContext.WithTimeout(TimeSpan.FromSeconds(5), out var timeoutToken);

// AmbientContext.CancellationToken is now linked to the 5-second timeout
await httpClient.GetAsync("https://api.example.com/data", AmbientContext.CancellationToken);
```

---

### 4. Zero-Ceremony Fast Logging & Structured Method Scopes

Access loggers on demand and create structured OpenTelemetry method scopes (`code.function`, `code.filename`, `code.filepath`, `code.lineno`) without constructor injection:

```csharp
public class OrderManager
{
    public void ProcessOrder(string orderId)
    {
        // Begins a structured method scope capturing OpenTelemetry caller metadata
        using var scope = AmbientContext.Log<OrderManager>().BeginMethodScope();

        // Resolves ILogger<OrderManager> from ambient LoggerFactory / Services
        AmbientContext.Log<OrderManager>().LogInformation("Processing order {OrderId}", orderId);
        
        // Or using caller instance type
        AmbientContext.Log(this).LogInformation("Order {OrderId} processed successfully", orderId);
    }
}
```

---

## Other Components

### Concurrent Factory Dictionary

```csharp
using ActDim.Practix.Collections.Concurrent;

var cache = new ConcurrentFactoryDictionary<string, UserProfile>(
    key => FetchUserProfileFromDatabase(key),
    StringComparer.OrdinalIgnoreCase
);

// Returns existing item or executes factory thread-safely
UserProfile profile = cache.GetOrAdd("user_42");
```

### Compression Manager

```csharp
using ActDim.Practix.Abstractions.Compression;

var compression = serviceProvider.GetRequiredService<ICompressionManager>();

byte[] originalData = System.Text.Encoding.UTF8.GetBytes("Payload data to compress");
byte[] compressedData = compression.Compress(originalData, CompressionFormat.GZip);
byte[] decompressedData = compression.Decompress(compressedData, CompressionFormat.GZip);
```

---

## Testing & Quality

- **Test Suite:** `ActDim.Practix.Common.Tests`
- **Total Tests:** 234 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Common.Tests/ActDim.Practix.Common.Tests.csproj
```

## License

This project is licensed under the [MIT License](../LICENSE).
