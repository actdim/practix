# ActDim.Practix.Json

`ActDim.Practix.Json` is a high-performance JSON serialization subsystem for .NET built on top of `System.Text.Json` (STJ), enhanced with a rich suite of converters, resolvers, and declarative attributes designed to make migration from **Newtonsoft.Json (Json.NET)** completely painless.

---

## Why ActDim.Practix.Json? (Painless Newtonsoft.Json Migration)

Migrating legacy codebases from `Newtonsoft.Json` to standard `System.Text.Json` is notoriously difficult due to STJ's strict type rules, lack of permissive string parsing, handling of `object` as `JsonElement`, and missing declarative policies. 

`ActDim.Practix.Json` bridges this gap by providing full behavioral compatibility with Newtonsoft.Json out of the box while retaining the extreme speed and low memory allocation of modern `System.Text.Json`.

---

## Key Features & STJ Extensions

### 1. Newtonsoft.Json Compatibility Layer
- **`ObjectJsonConverter` (`object` as CLR Primitives):** Standard STJ deserializes `object` properties into raw `JsonElement` structures. This converter maps them directly into native CLR types (`string`, `long`, `double`, `bool`, `Dictionary<string, object>`, `List<object>`), exactly like Json.NET.
- **`NewtonsoftCompatibleStringConverter`:** Permissively converts numbers, booleans, and nested tokens into `string` without throwing STJ schema mismatch exceptions.
- **`FloatingPointConverterFactory` & `NumberEnumConverterFactory`:** Lenient parsing for `float`, `double`, `decimal`, and numeric-backed `enum` types (supporting both quoted string numbers and numeric literals).
- **`CustomDateTimeConverter`:** Multi-format permissive `DateTime` / `DateTimeOffset` parsing across diverse legacy date representations.
- **`ImplicitOperatorConverterFactory`:** Automatically discovers and invokes custom C# `implicit` conversion operators during deserialization (ideal for Value Objects and Strongly-Typed IDs).
- **`ExceptionJsonConverter`:** Cleanly serializes deep .NET `Exception` hierarchies and inner exceptions without circular references or max depth exceptions.
- **`RuntimeTypeJsonConverter`:** Robust polymorphic type serialization supporting dynamic runtime types.

### 2. Declarative Attributes & Contract Resolvers
- **`[JsonIgnoreEmpty]` (`EmptyCollectionIgnoreResolver`):** Automatically omits empty collections, lists, arrays, and dictionaries from the serialized JSON output (mirroring Newtonsoft's `DefaultValueHandling.Ignore`).
- **`[JsonIgnoreDefault]` & `[JsonDefaultValue(val)]` (`DefaultValueAwareResolver`):** Ignores properties with default values or assigns declared fallback values when deserializing missing properties.
- **`[JsonNaming(namingPolicy)]` & `NamingPolicyResolver`:** Granular per-class or per-property naming policy customization, supporting `LowerCaseNamingPolicy`, `UpperCaseNamingPolicy`, `CamelCase`, and custom policies.

### 3. Extreme Performance with Zero Overhead
- **Expression Tree Delegate Compilation:** Fast, compiled Expression Trees are used for `CopyOptions` and fast property mutation, eliminating runtime reflection overhead and boxing.
- **Autonomous & Lightweight:** No external reflection libraries or bloated dependencies.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Practix.Json
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Practix.Json
```

---

## Dependency Injection Setup

Register `CoreJsonSerializer` as `IJsonSerializer`, `IStringSerializer`, `IBinarySerializer`, and `IStreamSerializer`:

```csharp
using ActDim.Practix.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Registers IJsonSerializer and related contracts
    services.AddPractixJson();
}
```

---

## Quick Start Examples

### 1. Basic Serialization & Deserialization

```csharp
using ActDim.Practix.Abstractions.Json;
using Microsoft.Extensions.DependencyInjection;

var serializer = serviceProvider.GetRequiredService<IJsonSerializer>();

var user = new UserProfile 
{ 
    Id = 42, 
    Name = "Alice", 
    Email = "alice@example.com" 
};

// Serialize to JSON string
string json = serializer.Serialize(user);

// Deserialize from JSON string
UserProfile restored = serializer.Deserialize<UserProfile>(json);
```

### 2. Painless Newtonsoft.Json Migration Patterns

```csharp
using ActDim.Practix.Json;
using System.Collections.Generic;

public class ApiResponse
{
    // Empty collections are omitted from JSON (like Newtonsoft DefaultValueHandling.Ignore)
    [JsonIgnoreEmpty]
    public List<string> Warnings { get; set; } = new List<string>();

    // Omitted if 0 (default value)
    [JsonIgnoreDefault]
    public int RetryCount { get; set; } = 0;

    // Injects "ACTIVE" if property is missing in input JSON
    [JsonDefaultValue("ACTIVE")]
    public string Status { get; set; }

    // Deserializes into Dictionary/primitives instead of raw JsonElement!
    public object DynamicPayload { get; set; }
}
```

---

## Testing & Quality

- **Test Suite:** `ActDim.Practix.Json.Tests`
- **Total Tests:** 102 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Json.Tests/ActDim.Practix.Json.Tests.csproj
```

---

## License

This project is licensed under the [MIT License](LICENSE).
