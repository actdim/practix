---
protocol: along
protocol_version: "2.2.18"
slug: architecture
title: System Architecture & Compilation Pipeline
type: architecture
created: 2026-09-03
updated: 2026-09-03
tags: [architecture, roslyn, scripting, compilation-pipeline, template-engine]
---

# System Architecture & Compilation Pipeline

`ActDim.Emitron` provides a unified dynamic C# compilation pipeline that converts runtime string templates and statement blocks into in-memory executable assemblies using the Roslyn compiler.

---

## High-Level Compilation Pipeline

```
[Template or Script Input]
           |
           +---> Syntax Analyzer & Rewriter
                     |-- Extracts inline '#r' and 'using' directives
                     |-- Rewrites template string into C# interpolation expression
                     |-- Maps model parameters to '@params' or custom identifier
                     |
           +---> Roslyn ScriptEngine (CSharpScript)
                     |-- Resolves metadata references from EmitronOptions
                     |-- Emits in-memory IL assembly
                     |-- Creates Func<object, T> delegate
                     |
           +---> Thread-Safe Concurrent Delegate Cache
                     |
           +---> High-Speed Native Re-Execution
```

---

## Subsystem Vector Decomposition

1. **Runtime String Interpolation ([`topic--runtime-string-interpolation.md`](./topic--runtime-string-interpolation.md))**:
   - Enables natural C# string interpolation (`$"Hello, {Name}!"`) loaded dynamically at runtime.
   - Supports direct variable references, format specifiers, and ternary logic.

2. **Roslyn Script Evaluation ([`topic--csharp-script-evaluation.md`](./topic--csharp-script-evaluation.md))**:
   - Compiles multi-statement C# logic and arbitrary expressions into reusable delegates.
   - Handles dynamic assembly loading and namespace resolution via `EmitronOptions`.
