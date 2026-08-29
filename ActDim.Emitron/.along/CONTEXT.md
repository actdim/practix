# Context

Current state snapshot of `ActDim.Emitron`.

## Overview
- Roslyn C# script engine (`Emitron`), template string compiler (`Interpolator`), and string interpolation extensions (`template.Interpolate(input)`).
- Full support for `#r "AssemblyName"` and `using Namespace;` directives with auto-injection of parameter bags.
- Programmatic assembly and namespace configuration via `EmitronOptions` (`SearchPaths`, `Assemblies`, `AssemblyReferences`, `Usings`).
- Direct parameter overloads for `assemblies:` and `usings:` in `Compile` and `Evaluate`.
- All 54 unit tests passing in `ActDim.Emitron.Tests` (100% success rate).
