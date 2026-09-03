---
protocol: along
protocol_version: "2.2.18"
slug: domain-model
title: Domain Model & Entities
type: domain-model
created: 2026-09-03
updated: 2026-09-03
tags: [domain-model, razor, parsing, templates]
---

# Domain Model & Entities

Core abstractions, parser components, and execution models in `ActDim.Emitron.Razor`.

---

## Domain Glossary

| Term | Definition | Primary Type |
| :--- | :--- | :--- |
| **Emitron Razor Facade** | High-level API for formatting and compiling dynamic Razor templates. | `EmitronRazor` |
| **Razor Parser** | AST tokenizer converting Razor directives into C# string-builder statements. | `RazorParser` |
| **String Extensions** | Fluent helper methods (`FormatRazor`, `CompileRazor`) extending `string`. | `StringExtensions` |

---

## Error Handling Contracts

1. **Unbalanced Braces in Razor Blocks**: Throws `FormatException` or `CompilationErrorException` during template parsing/compilation.
2. **Runtime Model Null Safety**: Accessing properties on a null model generates standard C# null-reference behavior inside the compiled script.
