# ActDim.Practix.Abstractions

`ActDim.Practix.Abstractions` defines the core contract interfaces, abstractions, and data models for the ActDim.Practix framework ecosystem.

## Features

- **Ambient Context Abstractions:** Defines `IAmbientContext` and `IAmbientContextProvider` for managing ambient request/operation state safely across asynchronous call flows (`AsyncLocal<T>`).
- **Compression & Archival Contracts:** Standardized interfaces for stream and buffer compression (`ICompressionManager`) supporting `GZip`, `BZip2`, `ZLib`, and archive inspection (`IArchiveInfo`, `IArchiveEntry`).
- **Data Access Interfaces:** Provider-agnostic interfaces for database operations (`IDbService`, `IDbConnectionProvider`, `ISqlDialect`, `ISequenceIdGenerator`, `IDbFetcher<T>`).
- **Blob Storage Contracts:** Extensible abstractions for blob storage providers (`IBlobStorage`, `IBlobStorageProvider`, `IBlob`).
- **Serialization & JSON Abstractions:** Core serialization contracts (`IJsonSerializer`, `IBinarySerializer`, `IStreamSerializer`, `IStringSerializer`).
- **Mapping & Messaging:** Generic mapper interfaces (`IMapper<TSource, TDestination>`) and messaging primitives.

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Practix.Abstractions
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Practix.Abstractions
```

## Quick Start

### Using Ambient Context Interfaces

```csharp
using ActDim.Practix.Abstractions.Context;

public class AuditService
{
    private readonly IAmbientContextProvider _contextProvider;

    public AuditService(IAmbientContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public void ProcessOperation()
    {
        var context = _contextProvider.Current;
        context.PushProperty("UserId", "user_12345");
        
        // Retrieve property later in the call stack
        if (context.Properties.TryGetValue("UserId", out var userId))
        {
            Console.WriteLine($"Current User: {userId}");
        }
    }
}
```

### Using Serialization Abstractions

```csharp
using ActDim.Practix.Abstractions.Json;

public class OrderProcessor
{
    private readonly IJsonSerializer _jsonSerializer;

    public OrderProcessor(IJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    public string SerializeOrder<T>(T order)
    {
        return _jsonSerializer.Serialize(order);
    }
}
```

## Dependency Injection

Implementations of these interfaces are available in concrete packages such as [`ActDim.Practix.Common`](https://www.nuget.org/packages/ActDim.Practix.Common) and [`ActDim.Practix.Json`](https://www.nuget.org/packages/ActDim.Practix.Json).

## License

This project is licensed under the [MIT License](LICENSE).
