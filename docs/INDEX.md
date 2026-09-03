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
    T_DOMAIN_MODEL["Domain Model & Vocabulary"]
    INDEX --> T_DOMAIN_MODEL
    T_SETUP_AND_WORKFLOW["Setup & Workflow"]
    INDEX --> T_SETUP_AND_WORKFLOW
```

---

## Articles

- **[System Architecture & Flow](./topic--architecture.md)** (architecture) `architecture`
- **[Domain Model & Vocabulary](./topic--domain-model.md)** (domain-model) `domain`
- **[Setup & Workflow](./topic--setup-and-workflow.md)** (setup-workflow) `workflow`, `setup`

---

## Related Context

- [AGENTS.md](../AGENTS.md): Active protocol conventions and rules.
- [.along/DECISIONS.md](../.along/DECISIONS.md): Architectural Decision Records.
- [.along/ISSUES.md](../.along/ISSUES.md): Active issue tracking board.
- [.along/HISTORY.md](../.along/HISTORY.md): Append-only project history log.
