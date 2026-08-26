# Context

Current state snapshot of `ActDim.BytePath.SqliteRegistry`.

## Overview
- SQLite-backed ACID blob registry (`SQLiteBlobRegistry`).
- Distributed locking, TTL metadata expiration, key prefix routing, and DI extension methods (`WithSQLiteRegistry()`, `AddSQLiteBlobRegistry()`).

