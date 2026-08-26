# ActDim.Emitron.Razor

`ActDim.Emitron.Razor` is a Roslyn-powered Razor template compilation and execution engine for .NET built on top of `ActDim.Emitron`.

## Features

- **Full Razor Template Support:** Render complex dynamic templates containing HTML, Plain Text, `@if / @else` conditionals, `@foreach / @for` loops, code blocks `@{ ... }`, and comments `@* ... *@`.
- **Model Property Access:** Reference properties seamlessly via `@Model.PropertyName` or `@params.PropertyName`.
- **Powered by Roslyn & Emitron:** Templates are transpiled into optimized C# scripts and compiled directly into memory using Roslyn Scripting with thread-safe delegate caching.
- **Fluent String Extensions:** Render templates directly using `template.FormatRazor(model)` or pre-compile using `template.CompileRazor()`.

## Quick Start Example

```csharp
using ActDim.Emitron.Razor;
using ActDim.Emitron.Razor.Extensions;

string razorTemplate = """
<h1>Welcome @Model.Name!</h1>
@if (Model.IsVip) {
    <p>Status: <strong>VIP Customer</strong></p>
} else {
    <p>Status: Regular Member</p>
}

<ul>
@foreach (var item in Model.Items) {
    <li>@item</li>
}
</ul>
""";

var model = new
{
    Name = "Alice",
    IsVip = true,
    Items = new[] { "Laptop", "Mouse", "Keyboard" }
};

// Compile and format in one step (cached on subsequent calls)
string output = razorTemplate.FormatRazor(model);

Console.WriteLine(output);
```

## License

MIT License.

