# ActDim.Emitron

`ActDim.Emitron` is a Roslyn-powered C# scripting evaluation engine and string template interpolation compiler for .NET.

## Features

- **Roslyn-Based C# Script Compilation:** Compile arbitrary C# code snippets or multi-statement blocks into highly efficient, reusable `Func<object, T>` delegates.
- **Collision-Free Property Binding:** Pass any anonymous object, DTO, or dictionary as input. Properties are bound safely to a dynamic parameter variable (`@params` by default, fully customizable).
- **Template Interpolation Compiler:** Parse and compile natural C# interpolated string templates (e.g., `$"Hello, {Name}! You have {Count} messages."`) into fast formatting delegates (`Interpolator`).
- **Concurrent Script Caching:** Script compilation occurs **once** per unique tuple `(code, inputParameterName, returnType)`. Compiled delegates are cached in memory for zero-overhead re-execution.
- **Fluent String Extension Helpers:** Execute template interpolation directly via `template.Interpolate(input)`.

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

### 1. Evaluating C# Expressions

```csharp
using ActDim.Emitron;

// Compile expression operating on @params
Func<object, string> greet = ScriptEngine.Compile<string>("@params.Name.ToUpper() + \"!\"");

// Execute cached delegate
string result = greet(new { Name = "world" });
Console.WriteLine(result); // Output: WORLD!
```

### 2. Multi-Statement Script Blocks

```csharp
using ActDim.Emitron;

var calculateTax = ScriptEngine.Compile<decimal>("""
    var price = (decimal)@params.Price;
    var rate = (decimal)@params.TaxRate;
    var total = price * (1 + rate);
    return Math.Round(total, 2);
""");

decimal taxResult = calculateTax(new { Price = 100.00m, TaxRate = 0.20m });
Console.WriteLine(taxResult); // Output: 120.00
```

### 3. Template Interpolation (Runtime C# String Templating)

In standard C#, string interpolation (`$"Hello, {Name}!"`) is hardcoded at compile-time directly in source code — it cannot be loaded dynamically from a database, config file, or external request at runtime.

`ActDim.Emitron` solves this by bringing **true C# string interpolation to runtime templates with near-native performance**:
- **Native C# Syntax & Rules:** Use the exact same interpolation syntax, format specifiers (`{Date:yyyy-MM-dd}`, `{Amount,8:C2}`), method calls, and property chains you already know in C#.
- **Lightning-Fast Compilation & Caching:** The template is compiled into native executable IL via Roslyn **once** on first use and cached. Subsequent executions run directly as compiled delegates without regex parsing or reflection interpretation overhead — delivering execution speeds comparable to compiled C# code.

```csharp
using ActDim.Emitron;

// Dynamic runtime template loaded from database, config, or user input
// Standard C# interpolated string — no special prefixes needed in placeholders!
string template = "$\"Order #{OrderId} for {Customer} is {Status} on {Date:yyyy-MM-dd}.\"";

// 1. Fluent execution: automatically compiles and caches on first call
string output1 = template.Interpolate(new 
{ 
    OrderId = 1001, 
    Customer = "Acme Corp", 
    Status = "SHIPPED", 
    Date = DateTime.UtcNow 
});

// 2. Or compile explicitly into a reusable, high-performance delegate
var formatter = Interpolator.Compile(template);

string output2 = formatter(new 
{ 
    OrderId = 1002, 
    Customer = "Globex", 
    Status = "PENDING", 
    Date = DateTime.UtcNow 
});
```

### 4. Custom Parameter Variable Name

```csharp
using ActDim.Emitron;

// Use 'model' instead of default '@params'
var eval = ScriptEngine.Compile<int>("model.A + model.B", inputParameterName: "model");
int sum = eval(new { A = 10, B = 20 });
```

## License

This project is licensed under the [MIT License](LICENSE).
