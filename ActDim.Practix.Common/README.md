# ActDim.Practix.Common

`ActDim.Practix.Common` provides shared utilities, concurrent collection factories, caching proxies, compression helpers, memory buffer management, and Microsoft Dependency Injection extensions for the ActDim.Practix framework.

## Features

- **Ambient Context Management:** `AmbientContext` and `AmbientContextProvider` for managing ambient variable states across asynchronous execution flows (`AsyncLocal<T>`).
- **Caching Proxies:** `MemoryCachingProxy` and `DistributedCachingProxy` offering simplified, resilient access to `IMemoryCache` and `IDistributedCache`.
- **Stream & Payload Compression:** `CompressionManager` for `GZip`, `BZip2`, and `ZLib` buffer/stream compression.
- **High-Performance Collections:** `ConcurrentFactoryDictionary` (thread-safe lock-free lookup/factory pattern), `WeakTable<K, V>`, and `CompositeKey`.
- **Memory Buffer Pooling:** Zero-allocation buffer management with `ArrayPoolBufferOwner` and `MemoryManager`.
- **Utility & Type Extensions:** Extension methods for `Encoding`, `ArraySegment`, `Stream`, `String`, `Enumerable`, `Task`, and `Guard` clauses.
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
using ActDim.Practix.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Register ambient context provider
    services.AddAmbientContext();

    // Register compression manager
    services.AddCompressionManager();

    // Register memory and distributed caching proxies
    services.AddMemoryCachingProxy();
    services.AddDistributedCachingProxy();
}
```

## Quick Start Examples

### 1. Concurrent Factory Dictionary

```csharp
using ActDim.Practix.Collections.Concurrent;

var cache = new ConcurrentFactoryDictionary<string, UserProfile>(
    key => FetchUserProfileFromDatabase(key),
    StringComparer.OrdinalIgnoreCase
);

// Returns existing item or executes factory thread-safely
UserProfile profile = cache.GetOrAdd("user_42");
```

### 2. Compression Manager

```csharp
using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

var compression = serviceProvider.GetRequiredService<ICompressionManager>();

byte[] originalData = System.Text.Encoding.UTF8.GetBytes("Payload data to compress");
byte[] compressedData = compression.Compress(originalData, CompressionFormat.GZip);
byte[] decompressedData = compression.Decompress(compressedData, CompressionFormat.GZip);
```

### 3. Ambient Context

```csharp
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

var contextProvider = serviceProvider.GetRequiredService<IAmbientContextProvider>();
var context = contextProvider.Current;

using (context.PushProperty("TenantId", "acme_corp"))
{
    // Operation logic executing inside tenant context
}
```

## Testing & Quality

- **Test Suite:** `ActDim.Practix.Common.Tests`
- **Total Tests:** 213 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Common.Tests/ActDim.Practix.Common.Tests.csproj
```

## License

This project is licensed under the [MIT License](LICENSE).
