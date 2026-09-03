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

Roslyn C# script engine (Emitron), template string compiler (Interpolator), and string interpolation extensions (template.Interpolate(input)). Full support for #r and using directives with auto-injection of parameter bags. Programmatic assembly and namespace configuration via EmitronOptions. Direct parameter overloads for ssemblies: and usings: in Compile and Evaluate.
