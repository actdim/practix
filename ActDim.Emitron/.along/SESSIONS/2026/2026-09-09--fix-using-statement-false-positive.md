---
protocol: along
date: 2026-09-09
slug: fix-using-statement-false-positive
agent: antigravity
branch: main
summary: Fixed false positive on using statements and declarations in Emitron ScriptInternals.FindInjectionIndex
issues_advanced: []
issues_completed: [bug--fix-using-statement-false-positive]
decisions: []
risks_logged: []
spikes_conducted: []
commit: unknown
milestone: v2.0.0-along-transition
---

# Session: Fix False Positive on Using Statements in Emitron

## Context & Problem
In `ActDim.Emitron/ScriptInternals.cs`, the scanner `FindInjectionIndex` checked for `"using "` without distinguishing using directives (`using Foo;`) from using statements (`using (var s = ...) { ... }`) or declarations. As a result, when a script contained a using statement, the scanner skipped past the opening expression to the next semicolon, placing the parameter injection `dynamic ctx = @params;` inside or after the statement, triggering Roslyn error CS0841 ("Cannot use local variable before it is declared").

## Changes Made
- In `ActDim.Emitron/ScriptInternals.cs`:
  - Implemented `TryConsumeUsingDirective`, `HasKeyword`, `SkipWhitespaceAndComments`, and `IsValidIdentifier`.
  - Accurately distinguished using directives from `using (...)` statements (rejecting parentheses before equals sign) and `using var` declarations.
  - Handled using alias directives (`using Alias = Target;`) strictly verifying single identifier syntax before `=`.
- In `Tests/Emitron.Tests/EmitronTests.cs`:
  - Added unit tests covering:
    - `Evaluate_WithUsingStatement_DoesNotTreatUsingAsDirective`
    - `Evaluate_WithUsingStatementNoBraces_DoesNotTreatUsingAsDirective`
    - `Evaluate_WithUsingStatementWithoutSpace_EvaluatesCorrectly`
    - `Evaluate_WithUsingDeclarationInBlock_AllowsAccessToContextParameter`
    - `Evaluate_WithUsingDirectivesAndUsingStatement_InjectsAfterDirectives`
    - `Evaluate_WithParameterInsideUsingExpression_EvaluatesCorrectly`

## Verification
- All 60 unit tests in `ActDim.Emitron.Tests` passed.
- All 653 unit tests in `ActDim.Practix.sln` passed.
