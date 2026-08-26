# Context

Current state snapshot of `ActDim.Emitron.Razor`.

## Overview
- Roslyn-powered Razor syntax template compiler (`EmitronRazor`, `RazorParser`).
- Supports multi-line templates with HTML/text, `@if / @else if / @else` conditionals, `@foreach / @for` loops, statement blocks `@{ ... }`, comments `@* ... *@`, parenthesized expressions `@(...)`, and property binding (`@Model.Property`).
- Integrated with `ActDim.Emitron`'s thread-safe delegate compilation and caching engine.
- Includes fluent string extensions `template.FormatRazor(model)` and `template.CompileRazor()`.
- 8 unit tests passing in `ActDim.Emitron.Razor.Tests` (100% success rate).

