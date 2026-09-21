---
protocol: along
slug: domain-model
title: Domain Model & Entity Ecosystem
type: domain-model
created: 2026-09-03
updated: 2026-09-09
tags: [domain-model, entities, schemas, dag, metadata]
---

# Domain Model & Entity Ecosystem

Core technical specification and documentation for ActDim.Practix.RepoDb.

## Overview

ActDim.Practix.RepoDb defines a domain model for type-safe, expression-based SQL query generation and execution on top of RepoDB.

## Core Domain Models

### `BoundSql`
An executable query context bound to an active `IDbConnection`.
- **Identity & State**: Holds `Connection` (`IDbConnection`) and `Sql` (`string`).
- **Inspection**: Overrides `ToString()` and provides `implicit operator string` returning the formatted, dialect-quoted SQL statement.
- **Execution Operations**:
  - `ExecuteQueryAsync<TResult>(object? param = null, ...)` -> `Task<IEnumerable<TResult>>`
  - `ExecuteNonQueryAsync(object? param = null, ...)` -> `Task<int>`
  - `ExecuteScalarAsync<TResult>(object? param = null, ...)` -> `Task<TResult?>`
  - `ExecuteReaderAsync(object? param = null, ...)` -> `Task<IDataReader>`
  - Synchronous counterparts: `ExecuteQuery<TResult>`, `ExecuteNonQuery`, `ExecuteScalar<TResult>`, `ExecuteReader`.

### `SqlTemplate`
A standalone, connection-independent SQL query definition.
- **Creation**: `SqlTemplate.Define<T1>`, `SqlTemplate.Define<T1, T2>`, `SqlTemplate.Define<T1, T2, T3>`, `SqlTemplate.Define<T1, T2, T3, T4>` (with or without `SqlDialect`).
- **Multi-Dialect Rendering**:
  - `Render(SqlDialect dialect)`: Renders without drivers using dialect quotation characters.
  - `Render<TConnection>()`: Renders using the `IDbSetting` registered for the connection type in RepoDB.
  - `Render(IDbSetting? dbSetting)`: Renders with explicit database settings.
  - `ToString()` / `Sql`: Renders using default quoting or configured dialect.
- **Connection Binding**: `Bind(IDbConnection connection)` binds the template to an active connection, resolving its dialect and returning a `BoundSql`.

### `SqlDialect` & `DialectDbSetting`
Dialect enumeration and lightweight `IDbSetting` implementation.
- `SqlDialect`: `Standard`, `Sqlite`, `SqlServer`, `PostgreSql`, `MySql`.
- `DialectDbSetting.For(dialect)`: Returns an `IDbSetting` implementing database quotation characters (`[ ]`, `" "`, ``` ` ` ```) without referencing driver packages.

### `SqlBuilder`
Static entry point and fluent builder.
- `SqlBuilder.For(dialect).Define<...>(...)`: Returns a `SqlTemplate` pre-configured with the target dialect.
- `SqlBuilder.Define<...>(...)`: Returns a standard dialect `SqlTemplate`.

### `SqlExpressionParser`
Internal AST visitor engine translating `Expression<Func<...>>` into SQL text.
- Inspects `MethodCallExpression` (`string.Format`), string concatenations (`+`), and raw string literals.
- Resolves member expressions to column names via RepoDb `PropertyCache` and `PropertyMappedNameCache`.
- Resolves parameter expressions to table names via RepoDb `ClassMappedNameCache`.
- Enforces table aliases based on parameter names for multi-table queries (`parameters.Length > 1`).
- Suppresses table aliases for single-table queries (`parameters.Length == 1`) or discard parameters (`_`).
- Caches compiled SQL templates in `ConcurrentDictionary` by expression structure.
