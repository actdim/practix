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

Core Dynamic Reflection:
  - **DynamicCodeManager**: Polished thread-safe manager for AssemblyBuilder and ModuleBuilder dynamic code emission with XML documentation and convenient GetModuleBuilder(assemblyName, moduleName) overload.
  - **DynamicTypeFactory**: Cleaned up obsolete security/CAS attributes, legacy partial trust code, added XML documentation and safe reflection type generation.
  - **ConcurrentFactoryDictionary**: Refactored in ActDim.Practix.Common, implemented IReadOnlyDictionary, removed over-constraining attributes, and added retry logic for factory failures.
