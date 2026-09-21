# ActDim.Practix.RepoDb

`ActDim.Practix.RepoDb` provides RepoDb extensions, lambda expression SQL translators, and best practices for ActDim.Practix.

## Features

- **Expression-Based Interpolated SQL Generator (`conn.DefineSql`):** Define SQL queries using clean C# string interpolation (`(c, o) => $"..."`) where properties and tables resolve automatically to RepoDB mapped names and table aliases without inner lambdas.
- **Bound Execution Context (`BoundSql`):** Captures the active database connection and compiled SQL. Provides `ExecuteQueryAsync<TResult>`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync<TResult>`, and `ExecuteReaderAsync` where developers specify only the target materialization type and query parameters.
- **Standalone SQL Templates (`SqlTemplate`):** Define reusable SQL templates without a live connection instance and render against specific database settings or bind to active connections.
- **Fluent Helper Extensions:** High-productivity helpers built on top of RepoDb (`ExecuteInTransactionAsync`, `NormalizeSqlPattern`).

## Installation

```bash
dotnet add package ActDim.Practix.RepoDb
```

## Quick Start

### 1. Single Table Query & Direct Column Resolution

```csharp
using ActDim.Practix.RepoDb.Extensions;

// Define and execute a query in one chain:
var users = await conn.DefineSql<User>(u => $"""
    SELECT {u.Id} AS Id, {u.FullName} AS Name, {u.Email}
    FROM {u}
    WHERE {u.IsActive} = @active
    """)
    .ExecuteQueryAsync<UserDto>(new { active = true });

// Directly resolve mapped column name in expressions:
var cmd = conn.DefineSql<User>(t => $"SELECT 1 AS {t.Id}");
// Result SQL: SELECT 1 AS [Id]
```

### 2. Multi-Table JOIN with Automatic Table Aliases

```csharp
// Lambda parameter names (c, o) become the SQL table aliases:
var orders = await conn.DefineSql<Customer, Order>((c, o) => $"""
    SELECT 
        {c.Id} AS CustomerId,
        {c.FullName} AS CustomerName,
        {o.OrderId} AS OrderId,
        {o.TotalAmount} AS Total
    FROM {c}
    JOIN {o} ON {c.Id} = {o.CustomerId}
    WHERE {o.Status} = @status
    """)
    .ExecuteQueryAsync<OrderSummaryDto>(new { status = "Completed" });
```

### 3. Inspection, Debugging & Logging via `ToString()`

```csharp
var bound = conn.DefineSql<Customer, Order>((c, o) =>
    $"SELECT {c.Id}, {o.TotalAmount} FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId}");

// Inspect the compiled SQL directly in debugger or logs:
logger.LogInformation("SQL to execute: {Sql}", bound.ToString());

// Execute scalar or non-query:
var count = await bound.ExecuteScalarAsync<int>();
```

### 4. Standalone SQL Templates & Dialect-Specific Rendering

```csharp
using ActDim.Practix.RepoDb;
using ActDim.Practix.RepoDb.Sql;

// Define a reusable template without a connection instance:
var template = SqlTemplate.Define<Customer, Order>((c, o) =>
    $"SELECT {c.Id}, {o.TotalAmount} FROM {c} JOIN {o} ON {c.Id} = {o.CustomerId}");

// Render without database drivers:
string pgSql = template.Render(SqlDialect.PostgreSql); // "c"."Id", "Customer" AS "c"
string mySql = template.Render(SqlDialect.MySql);      // `c`.`Id`, `Customer` AS `c`
string sqliteSql = template.Render(SqlDialect.Sqlite); // [c].[Id], [Customer] AS [c]

// Define directly targeting a dialect:
var pgQuery = SqlBuilder.For(SqlDialect.PostgreSql).Define<Customer, Order>((c, o) =>
    $"SELECT {c.Id} FROM {c}");

// Bind to an active connection when ready to execute:
var results = await conn.Bind(template).ExecuteQueryAsync<OrderDto>();
```

---

## AI-Assisted Development

Developed with [Along](https://github.com/actdim/along) - a provider-agnostic context and memory system for AI coding agents.

---

## License

This project is licensed under the [MIT License](../LICENSE).
