---
date: 2026-08-17
slug: script-evaluator-this-context-support
agent: antigravity
branch: main
commit: head
summary: Replaced internal __emitron_p requirement in ScriptEvaluator with clean this.Property, Context.Property, and Model.Property access.
---

# Session Log: ScriptEvaluator `this`, `Context`, and `Model` Support

## Changes Made & Rationale
- **`ScriptGlobals` DynamicObject Inheritance**:
  - `ScriptGlobals` in `ActDim.Emitron/ScriptInternals.cs` now inherits from `DynamicObject` and overrides `TryGetMember`.
  - Added public aliases `dynamic Context => this` and `dynamic Model => this`.
- **Script Preamble Rewrite**:
  - Pre-binds `Context`, `Model`, and rewrites `this.` $\rightarrow$ `Context.` in `ScriptEvaluator.CompileInternal`.
  - Replaces awkward internal variable `__emitron_p` with clean `this.Property`, `Context.Property`, and `Model.Property` in user scripts.

## Files Touched
- `ActDim.Emitron/ScriptInternals.cs`
- `ActDim.Emitron/ScriptEvaluator.cs`
- `Tests/Emitron.Tests/ScriptEvaluatorTests.cs`

## Verification
- Executed `dotnet test ActDim.Practix.sln`: all 488 tests passed across 6 test assemblies.
