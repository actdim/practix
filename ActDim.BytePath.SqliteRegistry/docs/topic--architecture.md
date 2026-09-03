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

SQLite-backed ACID blob registry (SQLiteBlobRegistry). Distributed locking, TTL metadata expiration, key prefix routing, and DI extension methods (WithSQLiteRegistry(), AddSQLiteBlobRegistry()).
