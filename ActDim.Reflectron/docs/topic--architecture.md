---
protocol: along
protocol_version: "2.2.18"
slug: architecture
title: System Architecture & Flow
type: architecture
created: 2026-09-03
updated: 2026-09-03
tags: [architecture, reflection, expression-trees, compilation-pipeline, design-patterns]
---

# System Architecture & Flow

`ActDim.Reflectron` provides high-performance, memory-safe reflection acceleration by converting runtime member lookups into compiled Expression Trees and managing target lifetimes through weak references.

---

## High-Level Architecture Pipeline

```
[Target Object / Type]
          |
          +---> .Reflect() Extension / Factory
                    |
      +-------------+-------------+
      |                           |
[Lookup in Static Cache]    [Expression Compiler Pipeline]
      |                           |
      |                     1. Build Expression Parameter Graph
      |                     2. Emit Property/Field/Method Access
      |                     3. LambdaExpression.Compile()
      |                           |
      +<--------------------------+
      |
[Wrap in IReflectron<T> with WeakReference<T>]
      |
[Native-Speed Getter/Setter/Method Invocation]
```

---

## Architectural Subsystems

1. **Compiled Expression Trees & Delegates ([`topic--compiled-expressions-and-delegates.md`](./topic--compiled-expressions-and-delegates.md))**:
   - Compiles strongly-typed lambda expressions into native function pointers (`Func<T, TValue>`, `Action<T, TValue>`).
   - Generates high-speed constructors and dynamic method invokers (`FastMethodCallDelegate`).

2. **Instance Reflection & Weak Memory Lifecycle ([`topic--dynamic-member-access.md`](./topic--dynamic-member-access.md))**:
   - `IReflectron<T>` encapsulates target objects inside `WeakReference<T>` to guarantee zero memory leaks.
   - Provides string indexer access (`reflector["Prop"]`) and strongly-typed lambda access (`reflector.Get(...)`, `reflector.Set(...)`).
