---
protocol: along
protocol_version: "2.2.8"
slug: domain-model
title: Domain Model & Vocabulary
type: domain-model
created: 2026-08-31
updated: 2026-09-02
tags: [domain]
---

# Domain Model & Vocabulary

Core domain terminology, data models, and state transitions.

- **Ambient Context Management**: `AmbientContext` acts as the direct holder of \AsyncLocal<ImmutableDictionary<string, object>>\ and singleton implementation of `IAmbientContext`. Static facade delegates 1-to-1 to `Current` and `AmbientContextExtensions`.
