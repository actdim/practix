# ActDim.Practix.Observability

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

### 2. Selective Provider Suppression

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

## Testing

Unit tests are located in `Tests/Observability.Tests/ActDim.Practix.Observability.Tests.csproj`. Run tests via:

```bash
dotnet test Tests/Observability.Tests/ActDim.Practix.Observability.Tests.csproj
```
