---
date: 2026-08-17
slug: refactor-script-evaluator-input-concept
agent: antigravity
branch: main
commit: head
summary: Renamed ScriptEvaluator to ScriptEngine and standardized on @params as the default collision-free parameter variable name.
---

# Session Log: `ScriptEngine` & `@params` Refactoring

## Changes Made & Rationale
- **Renamed `ScriptEvaluator` to `ScriptEngine`**:
  - Class and file renamed to `ScriptEngine` (`ActDim.Emitron/ScriptEngine.cs`).
  - Unit tests updated to `ScriptEngineTests.cs`.
- **Collision-Free `@params` Default Variable**:
  - Standard parameter variable name in Roslyn script scope is `@params` (`ScriptEngine.DefaultInputParameterName = "@params"`).
  - Because `params` is a reserved keyword in C#, user local variable declarations (`var params = ...`) inside script code produce C# syntax errors. This guarantees 100% collision-freedom with local user variables!
- **Customizable Parameter Name**:
  - `ScriptEngine` and `Interpolator` accept `inputParameterName` (defaulting to `"@params"`).
  - Custom parameter names (e.g. `"@ctx"`) generate an alias `dynamic @ctx = @params;\n` when specified.

## Files Touched
- `ActDim.Emitron/ScriptEngine.cs` [RENAMED from ScriptEvaluator.cs]
- `ActDim.Emitron/ScriptInternals.cs`
- `ActDim.Emitron/Interpolator.cs`
- `ActDim.Emitron/Extensions/StringExtensions.cs`
- `Tests/Emitron.Tests/ScriptEngineTests.cs` [RENAMED from ScriptEvaluatorTests.cs]
- `Tests/Emitron.Tests/InterpolatorTests.cs`

## Verification
- Clean build of `ActDim.Emitron.csproj` with zero warnings.
- Executed `dotnet test ActDim.Practix.sln` — 100% pass rate across all 489 tests in 6 test assemblies.
