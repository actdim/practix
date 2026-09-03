---
protocol: along
protocol_version: "2.2.18"
slug: setup-and-workflow
title: Setup, Installation & Developer Workflows
type: setup-workflow
created: 2026-08-31
updated: 2026-09-03
tags: [setup, workflow, testing, dependency-injection, installation]
---

# Setup, Installation & Developer Workflows

Installation instructions, dependency injection setup, and automated testing workflows for `ActDim.Practix.Common`.

---

## Package Installation

Install via the .NET CLI:
```bash
dotnet add package ActDim.Practix.Common
```

---

## Microsoft Dependency Injection Registration

`ActDim.Practix.Common` provides modular service registration extensions:

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Ambient Execution Context
    services.AddAmbientContext();

    // Stream & Payload Compression Manager
    services.AddCompressionManager();

    // Resilient Caching Proxies
    services.AddMemoryCache();
    services.AddMemoryCachingProxy();
    services.AddDistributedCachingProxy();
}
```

---

## Automated Testing & Quality Gates

The test suite is located in `Tests/Common.Tests/` using xUnit v3:

```bash
# Run tests with quiet output
dotnet test Tests/Common.Tests/ActDim.Practix.Common.Tests.csproj -v q
```

### Verified Test Subsystems (248 Tests Total):
- `Tests/Common.Tests/Context/`: `AmbientContextTests`, `AsyncLocalFlowTests`, `ScopeIsolationTests`.
- `Tests/Common.Tests/Pooling/`: `AsyncObjectPoolTests`, `DiscardAsyncTests`, `FaultTolerantDrainTests`.
- `Tests/Common.Tests/Caching/`: `MemoryCachingProxyTests`, `DistributedCachingProxyTests`.
- `Tests/Common.Tests/Compression/`: `CompressionManagerTests`, `ArchiveReaderTests`, `FormatSniffingTests`.
- `Tests/Common.Tests/Collections/`: `ConcurrentFactoryDictionaryTests`, `WeakTableTests`, `CompositeKeyTests`.
- `Tests/Common.Tests/Extensions/`: `StreamExtensionsTests`, `StringExtensionsTests`, `TaskExtensionsTests`.
- `Tests/Common.Tests/Runtime/`: `ReachabilityObserverTests`, `RandomIdTests`.
