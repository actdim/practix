---
protocol: along
protocol_version: "2.2.18"
slug: INDEX
title: Knowledge Base Topic Index
type: index
created: 2026-09-03
updated: 2026-09-03
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
    T_DOMAIN_MODEL["Domain Model & Entity Ecosystem"]
    INDEX --> T_DOMAIN_MODEL
    T_SETUP_AND_WORKFLOW["Setup, Installation & Agent Workflows"]
    INDEX --> T_SETUP_AND_WORKFLOW
```

---

## Articles

- **[System Architecture & Flow](./topic--architecture.md)** (architecture) `architecture`, `boundaries`, `providers`, `mcp`, `dashboard`
- **[Domain Model & Entity Ecosystem](./topic--domain-model.md)** (domain-model) `domain-model`, `entities`, `schemas`, `dag`, `metadata`
- **[Setup, Installation & Agent Workflows](./topic--setup-and-workflow.md)** (setup-workflow) `setup`, `workflow`, `installation`, `lifecycle`, `quality-gates`

---

## Related Context

- [AGENTS.md](../AGENTS.md): Active protocol conventions and rules.
- [.along/DECISIONS.md](../.along/DECISIONS.md): Architectural Decision Records.
- [.along/ISSUES.md](../.along/ISSUES.md): Active issue tracking board.
- [.along/HISTORY.md](../.along/HISTORY.md): Append-only project history log.
