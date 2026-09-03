---
protocol: along
protocol_version: "2.2.18"
slug: architecture
title: System Architecture & Flow
type: architecture
created: 2026-09-03
updated: 2026-09-03
tags: [architecture, razor, template-engine, transpilation, roslyn]
---

# System Architecture & Flow

`ActDim.Emitron.Razor` provides dynamic Razor template parsing and execution by transpiling Razor syntax into C# statement scripts evaluated via `ActDim.Emitron`.

---

## Architectural Pipeline

```
[Dynamic Razor Template]
          |
          +---> RazorParser (Syntax Tokenizer & Transpiler)
                    |
          +---> Emitted C# Script Body
                    |
          +---> Emitron Engine (Roslyn In-Memory Assembly Compilation)
                    |
          +---> Thread-Safe Cached Template Delegate
                    |
          +---> Fast HTML/Text String Generation
```

---

## Subsystems

1. **Razor Template Rendering ([`topic--razor-template-rendering.md`](./topic--razor-template-rendering.md))**:
   - Transpiles `@if`, `@foreach`, `@Model`, and code blocks `@{ ... }` into C# scripts.
   - Provides fluent string extensions (`FormatRazor`, `CompileRazor`).
