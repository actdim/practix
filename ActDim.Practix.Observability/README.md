# ActDim.Practix.Observability

A lightweight, OpenTelemetry-centric observability library for .NET applications built on top of `Microsoft.Extensions.Logging` and `System.Diagnostics.Activity`.

## Features

- **Zero-Ceremony Developer API:** Developers write standard `ILogger` calls and `logger.BeginScope()` without needing custom logger interfaces.
- **DI Decorator (`EventObservabilityLoggerFactory`):** Transparently decorates `ILoggerFactory` via DI container to inject `EventObservabilityBridge` for enriching logs and traces.
- **OpenTelemetry Activity Enrichment:** Automatically transforms scope objects, DTOs, and structured log parameters into flattened, dotted OpenTelemetry attributes (`user.id`, `order.price`).
- **CallContext Integration:** Automatically incorporates ambient execution properties from `ICallContextProvider` (`CallContext.PushProperty("name", value)`).
- **Status, Progress & Tagging Support:** Rich support for reporting operation progress, status text, icons, and arbitrary tags (`callContext.SetStatus("Downloading", icon: "🚀")`, `callContext.ReportProgress(45.5)`, `callContext.PushTags("billing", "urgent")`).
- **Selective Provider & Scope Suppression:** Dynamically suppress ambient `CallContext` properties, `IExternalScopeProvider` scopes, console loggers, or specific logger providers per async flow (`callContext.SuppressConsole()`, `callContext.SuppressProviders("File")`, `callContext.SuppressExternalScopes()`, `callContext.SuppressCallContext()`).
- **Provider Alias Resolution:** Automatically resolves provider aliases via official .NET `[ProviderAlias]` attributes or custom provider mappings.
- **External Scope Provider Support:** Implements `ISupportExternalScope` to capture external scopes from ASP.NET Core, HttpClient, gRPC, and EF Core.
- **Unified Event Model (`LogEvent`):** Unified domain event structure with optional activity tags.

## Registration

Register observability in your `IServiceCollection`:

```csharp
services.AddEventObservability(logging =>
{
    logging.AddConsole();
}, options =>
{
    options.IncludeExternalScopes = true;
    options.IncludeCallContext = true;
});
```

## Usage

### 1. Status, Progress & Icon Reporting

```csharp
using (callContext.SetStatus("Downloading Dataset", icon: "🚀"))
using (callContext.ReportProgress(45.5))
using (callContext.PushTags("billing", "priority-high"))
{
    logger.LogInformation("Importing rows into database");
}
```

### 2. Selective Provider Suppression

```csharp
// Suppress Console logger output while preserving OpenTelemetry Activity traces and other logger sinks
using (callContext.SuppressConsole())
{
    logger.LogInformation("Log without console output");
}

// Suppress specific providers by alias or name (e.g., "File", "Console")
using (callContext.SuppressProviders("File", "Console"))
{
    logger.LogInformation("Log without File and Console outputs");
}
```

## Testing

Unit tests are located in `Tests/Observability.Tests/ActDim.Practix.Observability.Tests.csproj`. Run tests via:

```bash
dotnet test Tests/Observability.Tests/ActDim.Practix.Observability.Tests.csproj
```
