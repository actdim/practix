---
slug: rename-script-evaluator-to-script-engine
type: debt
status: done
priority: medium
created: 2026-08-17
updated: 2026-08-17
---

# Tech Debt: Rename ScriptEvaluator to ScriptEngine

## Goal
Rename `ScriptEvaluator` class and file to `ScriptEngine` for clearer domain naming.

## Accomplished
- Renamed `ScriptEvaluator.cs` $\rightarrow$ `ScriptEngine.cs`.
- Updated all references across `ActDim.Emitron` and `Tests/Emitron.Tests`.
- Renamed `ScriptEvaluatorTests.cs` $\rightarrow$ `ScriptEngineTests.cs`.
