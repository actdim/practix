---
protocol: along
protocol_version: "2.2.26"
slug: INDEX
title: Knowledge Base Topic Index
type: index
created: 2026-09-08
updated: 2026-09-08
tags: [index, kb, topics, map]
---

# Knowledge Base Topic Index

Central entry point and cross-linked topic catalog for project documentation:

## Knowledge Graph & Topic Map

```mermaid
flowchart TD
    INDEX["Knowledge Base (INDEX)"]
    T_ARCHITECTURE["System Architecture & Flow"]
    INDEX --> T_ARCHITECTURE
    T_COMPILED_EXPRESSIONS_AND_DELEGATES["Compiled Expressions and Delegate Caching"]
    INDEX --> T_COMPILED_EXPRESSIONS_AND_DELEGATES
    T_DOMAIN_MODEL["Domain Model & Entities"]
    INDEX --> T_DOMAIN_MODEL
    T_DYNAMIC_MEMBER_ACCESS["Dynamic Member Access and Lifetime Management"]
    INDEX --> T_DYNAMIC_MEMBER_ACCESS
    T_SETUP_AND_WORKFLOW["Setup & Developer Workflow"]
    INDEX --> T_SETUP_AND_WORKFLOW
    T_ARCHITECTURE -.->|references| T_COMPILED_EXPRESSIONS_AND_DELEGATES
    T_ARCHITECTURE -.->|references| T_DYNAMIC_MEMBER_ACCESS
    T_COMPILED_EXPRESSIONS_AND_DELEGATES -.->|references| T_ARCHITECTURE
    T_COMPILED_EXPRESSIONS_AND_DELEGATES -.->|references| T_DYNAMIC_MEMBER_ACCESS
    T_DYNAMIC_MEMBER_ACCESS -.->|references| T_ARCHITECTURE
    T_DYNAMIC_MEMBER_ACCESS -.->|references| T_COMPILED_EXPRESSIONS_AND_DELEGATES
```

---

## Articles

- **[System Architecture & Flow](./topic--architecture.md)** (architecture) `architecture`, `reflection`, `expression-trees`, `compilation-pipeline`, `design-patterns`
- **[Compiled Expressions and Delegate Caching](./topic--compiled-expressions-and-delegates.md)** (topic) `reflection`, `expressions`, `delegates`, `caching`, `dynamic-method`, `constructors`
- **[Domain Model & Entities](./topic--domain-model.md)** (domain-model) `domain-model`, `interfaces`, `reflection`, `delegates`, `exceptions`
- **[Dynamic Member Access and Lifetime Management](./topic--dynamic-member-access.md)** (topic) `reflection`, `dynamic-access`, `indexer`, `weak-reference`, `memory-safety`, `factories`
- **[Setup & Developer Workflow](./topic--setup-and-workflow.md)** (setup-workflow) `setup`, `workflow`, `testing`, `nuget`, `benchmarks`

---

## Related Context

- [AGENTS.md](../AGENTS.md): Active protocol conventions and rules.
- [.along/DECISIONS.md](../.along/DECISIONS.md): Architectural Decision Records.
- [.along/ISSUES.md](../.along/ISSUES.md): Active issue tracking board.
- [.along/HISTORY.md](../.along/HISTORY.md): Append-only project history log.
