---
protocol: along
slug: architecture
title: System Architecture & Flow
type: architecture
created: 2026-09-03
updated: 2026-09-09
tags: [architecture, boundaries, providers, mcp, dashboard]
---

# System Architecture & Flow

Core technical specification and documentation for ActDim.Practix.RepoDb.

## Overview

ActDim.Practix.RepoDb provides high-productivity extensions and expression-based SQL query generation for RepoDb.

## Components

### 1. `SqlExpressionParser`
Translates C# lambda expressions (`(c, o) => $"..."`) into dialect-quoted SQL statements.
- Resolves `{c.Prop}` to `[c].[mapped_column]` via RepoDb `PropertyCache` and `PropertyMappedNameCache`.
- Resolves `{c}` to `[mapped_table] AS [c]` via RepoDb `ClassMappedNameCache`.
- Automatically assigns table aliases based on lambda parameter names for multi-table queries (JOINs) and suppresses aliases for single-table queries or `_` parameters.
- Caches compiled query delegates in `ConcurrentDictionary` for high performance.

### 2. `BoundSql`
Represents an executable SQL statement bound to an active `IDbConnection`.
- Exposes `Sql` and `ToString()` returning the compiled query text with database-specific quoting.
- Exposes `ExecuteQueryAsync<TResult>`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync<TResult>`, and `ExecuteReaderAsync` forwarding parameters and transactions directly to RepoDB.

### 3. `SqlTemplate`
Provides standalone query definitions without requiring an active database connection. Supports multi-dialect rendering via `Render(SqlDialect)` or `Render<TConnection>()` and late-binding via `Bind(IDbConnection)`.

### 4. `SqlDialect` & `DialectDbSetting`
Lightweight dialect abstraction providing database quotation characters without requiring external driver dependencies.
- `SqlDialect.PostgreSql`: Double quotes (`"identifier"`).
- `SqlDialect.MySql`: Backticks (``` `identifier` ```).
- `SqlDialect.Sqlite`, `SqlDialect.SqlServer`, `SqlDialect.Standard`: Square brackets (`[identifier]`).
- `DialectDbSetting`: Self-contained implementation of RepoDB's `IDbSetting`.

### 5. `SqlBuilder`
Static entry point for fluent, dialect-scoped query definitions (`SqlBuilder.For(SqlDialect.PostgreSql).Define(...)`) and neutral template definitions.

### 6. `DbConnectionSqlExtensions`
Provides `conn.DefineSql<T1>`, `conn.DefineSql<T1, T2>`, `conn.DefineSql<T1, T2, T3>`, and `conn.DefineSql<T1, T2, T3, T4>` extension methods on `IDbConnection`.
