---
slug: emitron-razor-engine
type: feat
status: done
priority: high
created: 2026-08-26
updated: 2026-08-26
---

# Add ActDim.Emitron.Razor project and Razor template compiler

## Description
Create `ActDim.Emitron.Razor` library and corresponding test project `ActDim.Emitron.Razor.Tests` to support full multi-line Razor syntax template compilation (conditionals `@if`, `@else if`, `@else`, loops `@foreach`, `@for`, code blocks `@{ }`, model access `@Model` / `@params`) powered by `Emitron` compilation and delegate caching engine.

## Acceptance Criteria
- [x] `ActDim.Emitron.Razor` project created targeting `net10.0` and referencing `ActDim.Emitron`.
- [x] Central Package Management (`Directory.Packages.props`) updated with any required dependencies.
- [x] `RazorInterpolator` / `EmitronRazor` class implemented providing `Compile` and `Format` methods.
- [x] Support for Razor control flow (`@if`, `@else`, `@foreach`, `@for`), expressions (`@Model.Property`), code blocks (`@{ ... }`), and comments (`@* ... *@`).
- [x] Integration with `Emitron` caching, `EmitronOptions`, dynamic input binding (`Model` / `@params`), and strongly typed models.
- [x] Comprehensive unit tests added to `Tests/Emitron.Razor.Tests` covering syntax features, caching, and model evaluation.
- [x] `ActDim.Practix.sln` updated with new projects, and all tests pass with 0 warnings.

