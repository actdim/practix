# ActDim.Practix.BlobManager — AI Context File

## Purpose

A blob storage library providing:
- **Metadata management** via SQLite (`blob_records` table)
- **Distributed read/write locking** via SQLite (`resource_locks` table)
- **File I/O** via file system (`FileSystemBlobDataStore`)
- **Expiration policies**: absolute, TTL, sliding

---

## Solution Layout

```
ActDim.Practix.BlobManager/
├── IBlobManager.cs           # Public API
├── IBlobDataStore.cs         # Public file I/O interface
├── IBlobStoreOptions.cs        # Public expiration options interface
├── BlobLockType.cs           # Enum: None / Read / Write
├── BlobRecord.cs             # SQLite ORM model + IAsyncDisposable
├── BlobManager.cs            # internal — delegates to IBlobDataStore + IBlobRegistry
├── IBlobRegistry.cs     # internal — SQLite lock + metadata contract
├── SQLiteBlobRegistry.cs# internal — lock engine + metadata persistence
├── FileSystemBlobDataStore.cs# public — FS implementation of IBlobDataStore
└── InternalsVisibleTo.cs     # exposes internals to test project

Tests/BlobManager.Tests/
└── BlobManagerTests.cs       # 7 xUnit v3 tests
```

---

## Key Concepts

### 1. Two-layer design

```
IBlobManager  (public)
    ├─ IBlobDataStore  → FileSystemBlobDataStore  (file ops)
    └─ IBlobRegistry   → SQLiteBlobRegistry       (locks + metadata)
```

`BlobManager` (internal) is a thin wrapper; all logic lives in `SQLiteBlobRegistry`.

### 2. Lock lifecycle — caller MUST `await using`

Every method that returns a `BlobRecord` holds a lock. The lock is released in `DisposeAsync` via the `OnDisposeAsync` callback set by `SQLiteBlobRegistry`.

| Method | Lock acquired | Lock released on dispose |
|--------|--------------|--------------------------|
| `GetOrCreateAsync` | Write | Updates `AccessedAt` + `UpdatedAt`, extends sliding expiry |
| `GetForReadingAsync` | Read | Updates `AccessedAt`, extends sliding expiry |
| `GetForWritingAsync` | Write | Updates `AccessedAt` + `UpdatedAt`, extends sliding expiry |

`GetForReadingAsync` and `GetForWritingAsync` throw `KeyNotFoundException` if the key does not exist — call `GetOrCreateAsync` first.

Read lock: allows concurrent readers, blocks writers.  
Write lock: exclusive — blocks all readers and writers.

Lock ownership is tracked via a `lockHeld` flag: on success it is transferred to `record.OnDisposeAsync`; on any exception the `finally` block releases it exactly once.

### 3. IBlobDataStore lock validation

`FileSystemBlobDataStore` validates `BlobRecord.LockType` before I/O:
- `Read` / `ReadAsync` — requires `LockType == Read || Write`
- `Write` / `WriteAsync` / `Append` / `AppendAsync` — requires `LockType == Write`

Passing a `BlobRecord` without a lock (or with the wrong lock type) throws `InvalidOperationException`.

### 4. `IBlobStoreOptions`

```csharp
public interface IBlobStoreOptions {
    DateTimeOffset? AbsoluteExpiration { get; set; }
    TimeSpan?       Ttl               { get; set; }
    TimeSpan?       SlidingExpiration { get; set; }
    string          ContentType       { get; set; }
    BlobLockType    LockType          { get; set; }
}
```

**Expiration priority** in `ApplyStorageOptions`: `AbsoluteExpiration` > `Ttl` > existing `SlidingExpiration`.  
`SlidingExpiration` additionally sets `SlidingExpirationSeconds` for future re-application on access.

**`ContentType`**, **`Hash`**, **`Metadata`**: each applied to the record when non-null/non-empty. Ignored otherwise (existing values kept).

**`lockType` parameter on `GetOrCreateAsync`** (not part of `IBlobStoreOptions`):
- New record → always `Write` regardless of `lockType`
- Existing record + `Read` → metadata saved under write lock, then downgraded to read lock before returning
- Existing record + `Write` (default) → write lock kept

### 5. SQLite locking (distributed)

Both `TryAcquireReadLockAsync` and `TryAcquireWriteLockAsync` use `BEGIN IMMEDIATE` + conditional `INSERT WHERE NOT EXISTS` + `changes()` to atomically acquire locks.

- Retry loop: every 100 ms until `_defaultTimeout` is reached → `TimeoutException`.
- Lock expiry stored in `resource_locks.expires_at`; stale locks are pruned at acquisition time.
- `effectiveTimeout` for lock expiry: `max(_defaultTimeout, 1s)` — minimum lock TTL is 1 second.
- All DB access is serialized via `SemaphoreSlim(1,1)` (`_dbSemaphore`).

### 6. SQLite schema

**`blob_records`** — blob metadata  
Columns: `key` TEXT (PK), `metadata` TEXT, `content_type` TEXT, `size` INTEGER, `hash` INTEGER, `created_at` INTEGER, `updated_at` INTEGER, `accessed_at` INTEGER, `sliding_expiration_seconds` INTEGER, `expires_at` INTEGER  
Index: `idx_blob_records_expires_at`

**`resource_locks`** — distributed lock entries  
Columns: `id` INTEGER (PK AUTOINCREMENT), `resource_id` TEXT, `is_write_lock` INTEGER (0=read/1=write), `locked_by` TEXT (UUID), `locked_at` INTEGER, `expires_at` INTEGER  
Index: `idx_resource_locks_resource_id`

All date columns in both tables are Unix timestamps (seconds, INTEGER). Comparisons pass `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` as a parameter — no `CURRENT_TIMESTAMP` or string formatting.

### 7. File path resolution (`FileSystemBlobDataStore`)

`BuildPath(record)` = `_basePath / SanitizeFileName(record.Key) + extension(record.Name)`

- `SanitizeFileName`: replaces `Path.GetInvalidFileNameChars()` with `_`
- Extension comes from `record.Name` (e.g. `.txt`, `.png`) — set `Name` before I/O if extension matters
- Falls back to `"blob"` if key is empty/whitespace

### 8. `BlobRecord` — stored vs ignored columns

`[Column]` (stored): `key`, `metadata` (TEXT), `content_type`, `size`, `hash`, `created_at` (INTEGER), `updated_at` (INTEGER), `accessed_at` (INTEGER), `sliding_expiration_seconds` (INTEGER), `expires_at` (INTEGER nullable)

`[Ignore]` (runtime-only): `CreatedAt`, `UpdatedAt`, `AccessedAt` (DateTimeOffset wrappers via `FromUnixTimeSeconds`/`ToUnixTimeSeconds`), `SlidingExpiration` (TimeSpan wrapper), `ExpiresAt` (DateTimeOffset? wrapper), `LockType`, `OnDispose`, `OnDisposeAsync`

Dates stored as Unix timestamps (seconds since epoch). No string formatting — `BlobRecord` is a near-POCO with only trivial type-conversion wrappers.  
`Metadata` (formerly `Name`) is a free-form TEXT field; `FileSystemBlobDataStore` uses `Path.GetExtension(record.Metadata)` to derive the file extension.

---

## Usage Pattern

```csharp
var metadataStore = new SQLiteBlobRegistry(dbPath, TimeSpan.FromSeconds(30));
var dataStore     = new FileSystemBlobDataStore(filesPath);
IBlobManager manager = new BlobManager(dataStore, metadataStore);

// Create record (also write-locked while handle is open)
await using (var record = await manager.GetOrCreateAsync("my-key", new MyOptions { Ttl = TimeSpan.FromHours(1) }, ct))
{
    record.Name = "report.pdf";
    await using var stream = await manager.DataStore.WriteAsync(record, ct);
    // write bytes to stream...
}  // lock released, timestamps updated

// Read
await using var readRecord = await manager.GetForReadingAsync("my-key", ct);
if (readRecord != null) {
    await using var stream = await manager.DataStore.ReadAsync(readRecord, ct);
    // read bytes...
}  // lock released
```

---

## Current Status (as of April 2026)

- [x] SQLite metadata store — create, update, delete, query
- [x] Read/write distributed locking via SQLite `resource_locks`
- [x] Expiration: absolute, TTL, sliding (refreshed on access/write)
- [x] File system data store with lock validation
- [x] `BlobRecord` IAsyncDisposable with dispose callbacks
- [x] `QueryAsync` with SQL LIKE pattern (normalize `*` → `%`)
- [x] `DeleteExpiredAsync`, `DeleteOlderThanAsync`, `CleanupAsync` (releases expired locks + deletes expired records)
- [x] 7 xUnit v3 tests covering locking, sliding expiry, round-trip I/O, lock enforcement
- [ ] DI registration helper (`IServiceCollection` extension / factory method)
- [ ] `IAsyncEnumerable<string>` variant of `QueryAsync` (commented out in `IBlobManager`)
- [ ] Nullable reference types enabled
- [ ] Tag-based query support
- [ ] Alternative `IBlobRegistry` implementations (PostgreSQL, Redis, etc.)
- [ ] Alternative `IBlobDataStore` implementations (Azure Blob, S3, etc.)

---

## Known Issues / Notes

- **`BlobManager` is `internal`** — no public factory or DI wiring yet. Tests construct it directly (`new BlobManager(dataStore, metadataStore)`). A future `AddBlobManager(IServiceCollection, ...)` extension is needed.
- **`GetForReadingAsync` / `GetForWritingAsync` throw `KeyNotFoundException`** if the key doesn't exist. Always `GetOrCreateAsync` first.
- **`GetOrCreateAsync` always holds a write lock** while the returned handle is open. Don't hold it longer than necessary.
- **`SQLiteBlobRegistry` constructor is synchronous** (`EnsureSchemaAsync().GetAwaiter().GetResult()`). Don't call from an async context that holds a sync lock.
- **`ToSqliteInterval`** private helper is defined but never used — dead code.
- **`StringFocus` project reference in tests** (`ActDim.Practix.StringFocus`) is declared in the `.csproj` but not used in `BlobManagerTests.cs` — likely a leftover.
- **`DeleteExpiredAsync` row count** uses `SELECT changes()` after `ExecuteAsync` — works under the semaphore but the count is returned from a separate statement; if the connection is somehow shared this could race. Currently safe.
- **Test timing**: `GetForReadingAsync_UpdatesAccessedAt` and `GetForWritingAsync_UpdatesUpdatedAt` use `Task.Delay(1100ms)` to ensure a timestamp difference. This may be slow on CI.
- **`Nullable` is disabled** across both projects (`<Nullable>disable</Nullable>`).

---

## Development Conventions

- **Namespace**: `ActDim.Practix.BlobManager` (all files)
- **Assembly**: `ActDim.Practix.BlobManager`
- **Target framework**: `net10.0`
- **Public surface**: `IBlobManager`, `IBlobDataStore`, `IBlobStoreOptions`, `BlobRecord`, `BlobLockType`, `FileSystemBlobDataStore`
- **Internal**: `BlobManager`, `IBlobRegistry`, `SQLiteBlobRegistry`
- `InternalsVisibleTo("ActDim.Practix.BlobManager.Tests")` enables direct test construction
- Always `await using` a `BlobRecord` — sync `Dispose()` only fires `OnDispose`, not `OnDisposeAsync`; prefer `DisposeAsync`
- The `BlobRecord.Name` field determines the file extension; set it before calling `DataStore.WriteAsync`
- Tests: xUnit v3, `TestContext.Current.CancellationToken`, isolated temp DB + data dir per test via `TestEnvironment`
