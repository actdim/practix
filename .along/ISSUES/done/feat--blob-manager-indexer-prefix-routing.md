---
protocol: along
slug: blob-manager-indexer-prefix-routing
type: feat
status: done
priority: high
created: 2026-09-22
updated: 2026-09-22
completed: 2026-09-22
agent: antigravity
tags: [storage, breaking-change, pkg-bytepath]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# blob-manager-indexer-prefix-routing

- status: done
- created: 2026-09-22
- updated: 2026-09-22
- completed: 2026-09-22

## Problem

`IBlobManager.DataStore` blindly returned `_dataStores[0]`, which was dangerous and ambiguous in multi-datastore setups. In addition, `BlobManager` constructor lacked validation against duplicate `KeyPrefix` registrations, and accessing a data store by key lacked an idiomatic C# indexer.

## Design & Implementation

1. Removed `IBlobDataStore DataStore { get; }` from `IBlobManager` and `BlobManager`.
2. Added indexer `IBlobDataStore this[string key] => GetDataStore(key);` to `IBlobManager` and `BlobManager`.
3. Validated `KeyPrefix` uniqueness in `BlobManager` constructor using `StringComparer.OrdinalIgnoreCase`.
4. Separated and pre-sorted stores into `_defaultStore` (catch-all) and `_prefixedStores` (sorted by prefix length descending) for deterministic longest prefix matching.
5. Migrated all test call sites and documentation examples to `manager[record.Key]`.

