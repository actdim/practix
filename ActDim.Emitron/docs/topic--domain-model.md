---
protocol: along
protocol_version: "2.2.18"
slug: domain-model
title: Domain Model & Vocabulary
type: domain-model
created: 2026-09-03
updated: 2026-09-03
tags: [domain-model, entities, options, compilation, parameters]
---

# Domain Model & Vocabulary

Core models, options, parameters, and error handling contracts in `ActDim.Emitron`.

---

## Domain Glossary

| Term | Definition | Primary Type |
| :--- | :--- | :--- |
| **Emitron Facade** | Primary entry point for compiling templates, expressions, and multi-statement scripts. | `Emitron` |
| **Interpolator** | Low-level syntax transformer converting template slots into C# interpolated expressions. | `Interpolator` |
| **Emitron Options** | Configuration container for assembly search paths, metadata references, and namespace imports. | `EmitronOptions` |
| **Script Internals** | Low-level Roslyn compiler wrapper and execution cache manager. | `ScriptInternals` |
| **Parameter Variable** | Identifier used inside script code to access input properties (defaults to `@params`). | `inputParameterName` |

---

## Error Handling & Compilation Guarantees

1. **Roslyn Compilation Errors**: Syntax or type errors in scripts throw `CompilationErrorException` containing diagnostic line and column details.
2. **Missing Reference Errors**: Referencing un-imported types or assemblies throws compilation diagnostic errors indicating the missing `#r` or `using` requirement.
3. **Null Input Tolerance**: Passing a null model to compiled templates evaluates null property references safely in C# string formatting.
