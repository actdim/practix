---
protocol: along
protocol_version: "2.2.26"
slug: bug--fix-using-statement-false-positive
type: bug
status: done
priority: high
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [emitron, scripting, parser, roslyn]
---

# Fix false positive on using statement/declaration in ScriptInternals

## Problem
In `ScriptInternals.FindInjectionIndex`, any code starting with `using ` was treated as a using directive.
When a script starts with or contains a using statement (`using (...) { ... }`) or using declaration (`using var x = ...;`),
the scanner scanned forward to the next semicolon, skipping past the start of executable code or inside the using block.
As a result, dynamic input parameter declarations (`dynamic ctx = @params;`) were injected after statements that already referenced the parameter,
causing Roslyn compilation error CS0841 ("Cannot use local variable before it is declared").

## Solution
1. Accurately distinguish using directives from using statements and declarations:
   - A using statement begins with `using (` or `using\s*\(`.
   - A using declaration begins with `using var ` or `using <Type> <ident> =`.
   - A using directive must be `using <namespace_or_type>;`, `using static <type>;`, `using <alias> = <target>;`, or `global using ...;`.
   - Ensure parentheses `(` before `=` or `{}` blocks immediately terminate directive scanning.
2. Added comprehensive unit tests in `ActDim.Emitron.Tests` covering:
   - `using (var s = ...)` block statements
   - `using (var s = ...) stmt;` single statement without braces
   - `using(var s = ...)` statement without space
   - `using var s = ...` inside blocks
   - `using System.IO;` directive combined with `using (...)` statement
   - Parameter usage inside `using (...)` header expression and body
