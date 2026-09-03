---
protocol: along
protocol_version: "2.2.8"
slug: architecture
title: System Architecture & Flow
type: architecture
created: 2026-08-31
updated: 2026-09-02
tags: [architecture]
---

# System Architecture & Flow

High-level architectural components, module boundaries, and execution models.

Roslyn-powered Razor syntax template compiler (EmitronRazor, RazorParser). Supports multi-line templates with HTML/text, conditionals, loops, statement blocks, comments, and property binding (@Model.Property). Integrated with ActDim.Emitron's thread-safe delegate compilation and caching engine. Includes fluent string extensions template.FormatRazor(model) and template.CompileRazor().
