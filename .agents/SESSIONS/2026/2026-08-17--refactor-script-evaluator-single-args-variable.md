---
date: 2026-08-17
slug: refactor-script-evaluator-single-args-variable
agent: antigravity
branch: main
commit: head
summary: Refactored ScriptEvaluator and Interpolator to use a single canonical, customizable @Args variable without legacy internal aliases.
---

# Session Log: Refactor ScriptEvaluator to `@Args` with Customizable Parameter Name

## Changes Made & Rationale
- **Single Canonical Parameter Variable (`@Args`)**:
  - Removed all legacy/internal aliases (`__emitron_p`, `Context`, `Scope`, `Model`, `Params`) from `ScriptGlobals`, `ScriptEvaluator`, `Interpolator`, and tests.
  - Default parameter variable in Roslyn script scope is now `@Args`.
- **Customizable Parameter Name Support**:
  - `ScriptEvaluator.Compile<T>(code, parameterName)` and `ScriptEvaluator.Evaluate<T>(code, parameters, parameterName)` allow specifying custom parameter variable names (e.g. `"@ctx"`, `"@model"`), defaulting to `"@Args"`.
  - `Interpolator.Compile(template, parameterName)`, `Interpolator.Format(template, parameters, parameterName)`, and `template.Interpolate(parameters, parameterName)` pass parameter variable names through to template rewriting and script compilation.
  - `this.Property` in script code continues to work smoothly by rewriting `this.` to `${parameterName}.`.

## Files Touched
- `ActDim.Emitron/ScriptInternals.cs`
- `ActDim.Emitron/ScriptEvaluator.cs`
- `ActDim.Emitron/Interpolator.cs`
- `ActDim.Emitron/Extensions/StringExtensions.cs`
- `Tests/Emitron.Tests/ScriptEvaluatorTests.cs`
- `Tests/Emitron.Tests/InterpolatorTests.cs`

## Verification
- Clean build of `ActDim.Emitron.csproj` with zero warnings.
- Executed `dotnet test ActDim.Practix.sln` — 100% pass rate across all 490 tests in 6 test assemblies.
