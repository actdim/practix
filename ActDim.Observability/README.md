# ActDim.Observability

A lightweight, OpenTelemetry-centric observability library for .NET applications built on top of `Microsoft.Extensions.Logging` and `System.Diagnostics.Activity`.

## Features

- **Zero-Ceremony Developer API:** Developers write standard `ILogger` calls and `logger.BeginScope()` without needing custom logger interfaces.
- **DI Decorator (`EventObservabilityLoggerFactory`):** Transparently decorates `ILoggerFactory` via DI container to inject `EventObservabilityBridge` for enriching logs and traces.
- **Activity & OpenTelemetry Enrichment:** Automatically transforms scope objects, DTOs, and structured log parameters into flattened, dotted OpenTelemetry attributes (`user.id`, `order.price`).
- **Auto Activity Creation on Scope:** Automatically starts an `Activity` span on `logger.BeginScope()` when no ambient span exists (`Activity.Current == null`), resolved via `observability.PushActivitySourceName(...)` or `EventObservabilityOptions.DefaultActivitySourceName`.
- **Ambient Context Separation:** `IAmbientContext` serves as a neutral ambient variable store. Only properties explicitly pushed via `IObservabilityContext` are exported to `Activity` tags.
- **Status & Progress Tracking:** First-class support for setting operation status text, icons, and progress percentage (`observability.SetStatus("Downloading", icon: "🚀")`, `observability.SetProgress(45.5)`).
- **Selective Provider & Scope Suppression:** Dynamically suppress console loggers, specific logger providers, or external scope export per async flow (`observability.SuppressConsole()`, `observability.SuppressProviders("File")`, `observability.SuppressExternalScopes()`).
- **Provider Alias Resolution:** Automatically resolves provider aliases via official .NET `[ProviderAlias]` attributes or custom provider mappings.

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Observability
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Observability
```

## Registration

Register observability in your `IServiceCollection`:

```csharp
services.AddEventObservability(logging =>
{
    logging.AddConsole();
}, options =>
{
    options.IncludeExternalScopes = false; // Default: false
});
```

## Usage

### 1. Status & Progress Reporting

```csharp
var observability = serviceProvider.GetRequiredService<IObservabilityContext>();

using (observability.SetStatus("Downloading Dataset", icon: "🚀"))
using (observability.SetProgress(45.5))
using (observability.Push("priority", "high"))
{
    logger.LogInformation("Importing rows into database");
}
```

### 2. Method Scopes with OpenTelemetry Semantic Conventions

Use `logger.BeginMethodScope()` to automatically capture the executing method name, source file, and line number without manual string formatting. Scope properties strictly adhere to the [OpenTelemetry Source Code Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/attributes-registry/code/):

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessOrderAsync(string orderId)
    {
        // Automatically captures code.function="ProcessOrderAsync", code.filename="OrderService.cs", code.lineno=...
        using (_logger.BeginMethodScope())
        {
            _logger.LogInformation("Processing order {OrderId}", orderId);
        }

        // Merge custom state with caller code context
        using (_logger.BeginMethodScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
        {
            _logger.LogInformation("Order completed");
        }
    }
}
```

| Scope Key | Constant (`ObservabilityTagNames.Code`) | Description |
| :--- | :--- | :--- |
| `code.function` | `ObservabilityTagNames.Code.Function` | Caller method or member name |
| `code.filename` | `ObservabilityTagNames.Code.FileName` | File name (e.g. `OrderService.cs`) |
| `code.filepath` | `ObservabilityTagNames.Code.FilePath` | Full source file path |
| `code.lineno` | `ObservabilityTagNames.Code.LineNumber` | Source code line number |

*Why OpenTelemetry Semantic Conventions?* Standard attribute names (`code.function`, `code.filepath`, `code.lineno`) enable APM tools, log aggregators, and distributed trace visualizers (Jaeger, Grafana Tempo, Datadog, Dynatrace, and .NET Aspire) to natively index, filter, and navigate directly to source code locations.

### 3. Selective Provider Suppression

```csharp
// Suppress Console logger output while preserving Activity traces and other logger sinks
using (observability.SuppressConsole())
{
    logger.LogInformation("Log without console output");
}

// Suppress specific providers by alias or name (e.g., "File", "Console")
using (observability.SuppressProviders("File", "Console"))
{
    logger.LogInformation("Log without File and Console outputs");
}
```

## Testing & Quality

- **Test Suite:** `ActDim.Observability.Tests`
- **Total Tests:** 28 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Observability.Tests/ActDim.Observability.Tests.csproj
```

## License

This project is licensed under the [MIT License](LICENSE).
