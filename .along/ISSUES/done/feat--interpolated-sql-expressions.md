---
protocol: along
protocol_version: "3.8.0"
slug: feat--interpolated-sql-expressions
type: feat
status: done
completed: 2026-09-21
priority: high
created: 2026-09-21
updated: 2026-09-21
agent: antigravity
tags: [repodb, sql, expressions, interpolation, ast, pkg-practix-repodb]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Expression-Based Interpolated SQL Generator and Bound Execution

Add expression-based SQL definition and execution capabilities to `ActDim.Practix.RepoDb`. Allow defining SQL queries using clean C# string interpolation (`(c, o) => $"..."`) where properties and table references are automatically resolved to RepoDB mapped names and aliases without inner lambdas.

## Requirements Traceability Matrix
- REQ-1: `BoundSql` executable context holding `IDbConnection`, compiled SQL, and providing execution methods (`ExecuteQueryAsync<TResult>`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync<TResult>`, `ExecuteReaderAsync`) accepting only target materialization type and parameters.
- REQ-2: Expression AST Parser (`SqlExpressionParser`) translating `(t1, ...) => $"..."` into SQL text: resolving `{t.Prop}` to `[alias].[mapped_column]` via RepoDb `PropertyMappedNameCache`, resolving `{t}` to `[mapped_table] AS [alias]` via `ClassMappedNameCache`, and extracting aliases from lambda parameter names.
- REQ-3: Dialect-aware identifier quoting based on `conn.GetDbSetting()` or `DbSettingMapper.Get(conn.GetType())`.
- REQ-4: `ToString()` on `BoundSql` and `SqlTemplate` returns formatted SQL string for inspection, logging, and debugging.
- REQ-5: Overloaded `conn.DefineSql` extension methods supporting 1, 2, 3, and 4 entity types (`T1`, `T1, T2`, `T1, T2, T3`, `T1, T2, T3, T4`).
- REQ-6: High-performance caching for parsed SQL AST expressions to guarantee sub-microsecond subsequent executions.
- REQ-7: Standalone `SqlTemplate.Define<...>` allowing template creation without a live connection instance and rendering via `template.Render<TConnection>()` or `template.Render(IDbSetting)`.

## Acceptance Criteria
- [x] `conn.DefineSql<T1>(...)` to `conn.DefineSql<T1, T2, T3, T4>(...)` produce correct SQL matching RepoDb mappings and aliases.
- [x] Properties mapped via attributes (`[Map]`, `[Column]`) or `FluentMapper` resolve to database column names.
- [x] Table aliases are automatically bound from lambda parameter names.
- [x] `BoundSql.ToString()` and `BoundSql.Sql` return the exact generated SQL string.
- [x] Execution methods on `BoundSql` forward parameters to RepoDB and correctly materialize results.
- [x] Unit tests cover single table, multi-table joins, custom column mappings, ToString(), and execution against SQLite in-memory database.
