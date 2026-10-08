---
protocol: along
date: 2026-08-18
slug: assemblies-and-usings-support
agent: antigravity
branch: main
commit: pending
summary: Implemented Assemblies and Usings support with SearchPaths, inline #r and using directives, and EmitronOptions in ActDim.Emitron
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Assemblies and Usings Support in ActDim.Emitron

## Summary of Changes
- Implemented `EmitronOptions` with `SearchPaths`, `Assemblies`, `AssemblyReferences`, and `Usings` configuration.
- Configured Roslyn `ScriptMetadataResolver` and `ScriptSourceResolver` to resolve assembly references from `SearchPaths`.
- Implemented `ScriptInternals.PrepareScriptSource` to correctly inject `dynamic <paramName> = @params;` after all `#` directives (`#r`, `#load`, etc.) and `using` statements.
- Added API overloads to `Emitron.Compile<T>`, `Emitron.Evaluate<T>`, `Interpolator.Compile`, and `Interpolator.Format` accepting `EmitronOptions`, `assemblies: [...]`, and `usings: [...]`.
- Added unit tests in `EmitronTests.cs` (54 tests passing, 100% success rate).
- Updated README.md documentation with examples.

## Files Touched
- `ActDim.Emitron/EmitronOptions.cs` (NEW)
- `ActDim.Emitron/ScriptInternals.cs`
- `ActDim.Emitron/Emitron.cs`
- `ActDim.Emitron/Interpolator.cs`
- `ActDim.Emitron/README.md`
- `Tests/Emitron.Tests/EmitronTests.cs`
- `README.md`

## Decisions
- ADR #2: Support Assemblies and Usings via Script Directives and EmitronOptions
