# ActDim.Practix.Json

`ActDim.Practix.Json` is a high-performance JSON serialization subsystem for the ActDim.Practix framework built on top of `System.Text.Json` and optimized using [`ActDim.Reflectron`](https://www.nuget.org/packages/ActDim.Reflectron) expression tree property setters and cached type metadata.

## Features

- **Reflectron-Optimized Deserialization:** Leverages fast compiled expression-tree property setters and metadata caching via `ActDim.Reflectron` to bypass standard reflection overhead.
- **Advanced Custom Converters:** Standardized converters for `Exception` objects, floating-point numbers, number-backed enums, implicit operators, runtime type polymorphism, and Newtonsoft-compatible string parsing.
- **Custom Naming Policies & Resolvers:** Configurable naming policies (`LowerCaseNamingPolicy`, `UpperCaseNamingPolicy`, `NamingPolicyResolver`) and type contract resolvers.
- **Declarative Attributes:**
  - `[JsonDefaultValue(value)]`: Specify fallback default property values.
  - `[JsonIgnoreDefault]`: Omit properties carrying default target values during serialization.
  - `[JsonIgnoreEmpty]`: Ignore empty collections, arrays, and dictionaries.
  - `[JsonNaming(namingPolicy)]`: Per-class or per-property naming policy overriding.
- **Microsoft Dependency Injection Support:** Simple DI registration via `services.AddPractixJson()`.

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Practix.Json
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Practix.Json
```

## Dependency Injection Setup

Register `CoreJsonSerializer` as `IJsonSerializer` in Microsoft Dependency Injection:

```csharp
using ActDim.Practix.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddPractixJson();
}
```

## Usage Examples

### 1. Basic Serialization & Deserialization

```csharp
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;

var serializer = serviceProvider.GetRequiredService<IJsonSerializer>();

var user = new UserProfile { Id = 42, Name = "Alice", Email = "alice@example.com" };

// Serialize to JSON string
string json = serializer.Serialize(user);

// Deserialize from JSON string
UserProfile restoredUser = serializer.Deserialize<UserProfile>(json);
```

### 2. Using Custom Attributes

```csharp
using ActDim.Practix.Json;
using System.Collections.Generic;

public class APIResponse
{
    [JsonIgnoreEmpty]
    public List<string> Warnings { get; set; } = new List<string>();

    [JsonIgnoreDefault]
    public int RetryCount { get; set; } = 0;

    [JsonDefaultValue("SUCCESS")]
    public string Status { get; set; }
}
```

## License

This project is licensed under the [MIT License](LICENSE).
