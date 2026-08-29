# ActDim.Emitron

`ActDim.Emitron` is a Roslyn-powered C# scripting evaluation engine and string template interpolation compiler for .NET.

## Features

- **Runtime C# String Interpolation:** Use natural, standard C# string interpolation syntax (`$"Hello, {Name}!"`) directly on dynamic strings loaded from databases, files, or external inputs without compilation overhead.
- **Direct Variable Referencing:** Reference model properties directly by name (`{Name}`, `{Date:yyyy-MM-dd}`, `{Amount:C2}`) without mandatory `@params.` prefixes.
- **Advanced Expressions & Logic in Templates:** Use format specifiers, method calls (`{Name.Trim().ToUpper()}`), ternary conditions (`{(IsActive ? "ACTIVE" : "DISABLED")}`), or explicit `@params` access inside interpolation slots.
- **Roslyn-Based C# Script Compilation:** Compile arbitrary C# statement blocks or expressions into reusable `Func<object, T>` delegates via `Emitron.Compile<T>`.
- **Single Unified Facade (`Emitron`):** Access template interpolation, expression evaluation, and multi-statement scripts from a single, clean API entry point.
- **Concurrent IL Caching:** Compilation occurs **once** per unique template/script and is cached in memory for zero-overhead re-execution with performance comparable to native compiled C#.
- **Fluent String Extension Helpers:** Format dynamic templates directly via `template.Interpolate(input)`.

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Emitron
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Emitron
```

## Quick Start Examples

### 1. Basic Template Interpolation (Natural C# Syntax)

In standard C#, string interpolation (`$"Hello, {Name}!"`) is hardcoded at compile-time directly in source code. `ActDim.Emitron` enables **true runtime C# string interpolation**: variables are referenced directly in interpolation slots just like in native C#, with full support for format specifiers:

```csharp
using ActDim.Emitron;

// 1. Dynamic template loaded at runtime (from DB, config, or user input):
// Properties are accessed directly by name: no '@params.' prefix required!
string template = """$"Hello, {Name}! Today is {Date:yyyy-MM-dd} and your balance is ${Balance:N2}." """;

// Execute directly via fluent extension method (compiles & caches on first call):
string greeting = template.Interpolate(new 
{ 
    Name = "Alice", 
    Date = DateTime.Today, 
    Balance = 1540.75m 
});

Console.WriteLine(greeting);
// Output: Hello, Alice! Today is 2026-08-18 and your balance is $1,540.75.

// 2. Or pre-compile explicitly into a reusable, zero-overhead delegate:
var formatter = Emitron.CompileTemplate("""$"Welcome, {FirstName} {LastName}!" """);
string welcome = formatter(new { FirstName = "John", LastName = "Doe" });
// Output: Welcome, John Doe!
```

---

### 2. Advanced Template Interpolation (Expressions, Methods & Conditionals)

Interpolation slots support full C# expressions, method invocations, ternary operators, and optional `@params` references:

```csharp
using ActDim.Emitron;

// Template with method calls, ternary conditions, and formatted values (no quote escaping needed!):
string invoiceTemplate = """$"Invoice #{InvoiceId:D6} | Client: {ClientName.Trim().ToUpper()} | Total: ${Total:N2} | Status: {(IsPaid ? "PAID" : "PENDING")}" """;

string invoice = invoiceTemplate.Interpolate(new 
{ 
    InvoiceId = 42, 
    ClientName = "  acme corp  ", 
    Total = 8450.00m, 
    IsPaid = true 
});

Console.WriteLine(invoice);
// Output: Invoice #000042 | Client: ACME CORP | Total: $8,450.00 | Status: PAID
```

---

### 3. Evaluating C# Expressions & Logic (`Emitron.Compile` / `Emitron.Evaluate`)

For arbitrary C# logic, expressions, and algorithms, use `Emitron.Compile<T>` or `Emitron.Evaluate<T>`. Inside the code block, properties are bound to the `@params` variable (or a custom named parameter):

```csharp
using ActDim.Emitron;

// One-shot evaluation:
int result = Emitron.Evaluate<int>("(int)@params.A * (int)@params.B", new { A = 6, B = 7 });
Console.WriteLine(result); // Output: 42

// Reusable compiled delegate with multi-statement business logic:
var calculateDiscount = Emitron.Compile<decimal>("""
    decimal subtotal = (decimal)@params.Subtotal;
    int tier = (int)@params.Tier;
    bool isVip = tier >= 2;

    return isVip ? subtotal * 0.15m : subtotal * 0.05m;
""");

decimal discount = calculateDiscount(new { Subtotal = 1000m, Tier = 3 });
Console.WriteLine($"Discount: {discount}"); // Output: Discount: 150.00
```

---

### 4. Custom Parameter Name in Templates & Scripts

You can bind caller inputs to a custom variable name (e.g. `user`, `order`, `ctx`) instead of the default:

```csharp
using ActDim.Emitron;

// 1. In templates:
var alertFormatter = Emitron.CompileTemplate(
    """$"Server '{ctx.Host}' CPU load: {ctx.CpuLoad * 100:F1}% (Memory: {ctx.MemoryMb}MB)" """,
    inputParameterName: "ctx"
);

string alert = alertFormatter(new { Host = "srv-01", CpuLoad = 0.854, MemoryMb = 4096 });
Console.WriteLine(alert);
// Output: Server 'srv-01' CPU load: 85.4% (Memory: 4096MB)

// 2. In C# script blocks:
var taxCalculator = Emitron.Compile<decimal>(
    "return (decimal)order.Price * (1m + (decimal)order.TaxRate);",
    inputParameterName: "order"
);

decimal total = taxCalculator(new { Price = 100m, TaxRate = 0.20m });
Console.WriteLine(total); // Output: 120.00
```

### 5. Assemblies, Namespaces & Directives (`#r` and `using`)

`Emitron` fully supports Roslyn assembly directives (`#r "AssemblyName"`), namespace imports (`using Namespace;`), and custom search paths via `EmitronOptions`:

```csharp
using ActDim.Emitron;

// 1. Inline Roslyn directives inside script code:
var parseJson = Emitron.Compile<string>("""
    #r "System.Text.Json"
    using System.Text.Json;

    var doc = JsonDocument.Parse((string)@params.Json);
    return doc.RootElement.GetProperty("status").GetString();
""");

string status = parseJson(new { Json = """{"status":"active"}""" });
// Output: "active"

// 2. Or pre-configured via EmitronOptions / Emitron.DefaultOptions:
var options = new EmitronOptions()
    .AddSearchPaths(@"C:\MyPlugins\Assemblies")
    .AddAssemblies(typeof(System.Text.Json.JsonDocument).Assembly)
    .AddUsings("System.Text.Json");

var evaluator = Emitron.Compile<bool>("""
    var doc = JsonDocument.Parse((string)@params.Json);
    return doc.RootElement.GetProperty("valid").GetBoolean();
""", options);

// 3. Or pass assemblies/types and usings directly to Compile / Evaluate:
var calc = Emitron.Compile<int>("""
    return JsonDocument.Parse((string)@params.Json).RootElement.GetProperty("id").GetInt32();
""",
types: [typeof(System.Text.Json.JsonDocument)],
usings: ["System.Text.Json"]);

int id = calc(new { Json = """{"id":42}""" }); // Output: 42
```

---

## Testing & Quality

- **Test Suite:** `ActDim.Emitron.Tests`
- **Total Tests:** 54 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Emitron.Tests/ActDim.Emitron.Tests.csproj
```

---

## License

This project is licensed under the [MIT License](LICENSE).


