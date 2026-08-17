---
slug: feat--emitron-tests
type: feat
status: done
priority: high
created: 2026-08-15
updated: 2026-08-17
---

# feat: Create ActDim.Emitron tests

## Problem
`ActDim.Emitron` (`InterpolationFormatter` and `ScriptEvaluator`) needed comprehensive test coverage in the solution.

## Acceptance Criteria
- Test project `Tests/Emitron.Tests/` added referencing `ActDim.Emitron`.
- Tests cover:
  - InterpolationFormatter template parsing, format specifiers, and slot expression evaluation.
  - ScriptEvaluator expression evaluation, multi-statement blocks, type parameterization (`Compile<T>`).
  - Parameter mapping via anonymous types, POCOs, and `IDictionary<string, object>`.
  - Roslyn compilation caching per template/expression string.
  - Exception handling for null/empty templates and invalid C# code.
