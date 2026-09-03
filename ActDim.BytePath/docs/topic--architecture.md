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

- **State**: `IBlobDataStore.PutAsync` is the verified whole-blob create-or-replace operation (101/101 BlobManager tests green). Multipart upload sessions are planned, not implemented.
- **Shape**: two layers that do not know each other: `SQLiteBlobRegistry` (metadata + locks) and `FileSystemBlobDataStore` (content). `BlobManager` is the only place that sees both, so `ReconcileContentAsync` and all four deletion paths live there.
- **Key format**: \:\ (colon) is the default URL-safe logical hierarchy separator per RFC 3986 `pchar` (#011). `FileSystemBlobDataStoreOptions.HierarchySeparator` controls on-disk directory splitting (defaults to \':'\, \
ull\ for uniform hash-sharding). `EscapeFileName` uses reversible \%XX\ escaping and protects Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
