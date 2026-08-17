# ActDim.BytePath.SqliteRegistry

SQLite metadata registry implementation for `ActDim.BytePath`.

## Features
- **ACID Metadata Registry**: Backed by SQLite (`sqlite-net-pcl`) with transactions (`BEGIN IMMEDIATE`) ensuring zero concurrency race conditions.
- **Distributed Read/Write Locks**: Pessimistic cooperative locking with expiration and automatic cleanup.
- **TTL & Sliding Expiration**: Direct querying of expired keys and age-based key scanning.

## Installation

```bash
dotnet add package ActDim.BytePath.SqliteRegistry
```

## Quick Start

```csharp
services.AddBlobManager(builder =>
{
    builder.WithSQLiteRegistry(options =>
    {
        options.DatabaseName = "blobs.db";
        options.BaseDirectory = "./my-blobs";
    });
});
```
