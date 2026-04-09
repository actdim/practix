using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SQLite;

namespace ActDim.Practix.BlobManager
{
    [Table("blob_records")]
    internal class BlobRecordTransport
    {
        [PrimaryKey]
        [Column("key")]
        public string Key { get; set; }

        [Column("metadata")]
        public string Metadata { get; set; }

        [Column("content_type")]
        public string ContentType { get; set; }

        [Column("size")]
        public long? Size { get; set; }

        [Column("hash")]
        public string Hash { get; set; }

        [Column("created_at")]
        public long CreatedAtUnix { get; set; }

        [Column("updated_at")]
        public long UpdatedAtUnix { get; set; }

        [Column("accessed_at")]
        public long AccessedAtUnix { get; set; }

        [Column("sliding_expiration_seconds")]
        public long? SlidingExpirationSeconds { get; set; }

        [Column("expires_at")]
        public long? ExpiresAtUnix { get; set; }
    }

    internal class SQLiteBlobRegistry : IBlobRegistry
    {
        private readonly SQLiteAsyncConnection _db;
        private readonly TimeSpan _defaultTimeout;
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

        public SQLiteBlobRegistry(string connectionString, TimeSpan defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", nameof(connectionString));
            }

            _defaultTimeout = defaultTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : defaultTimeout;
            _db = new SQLiteAsyncConnection(connectionString);

            EnsureSchemaAsync().GetAwaiter().GetResult();
        }

        public async Task DeleteAsync(string key, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.ExecuteAsync("DELETE FROM resource_locks WHERE resource_id = ?;", key);
                await _db.ExecuteAsync("DELETE FROM blob_records WHERE key = ?;", key);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<int> DeleteExpiredAsync(CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.ExecuteAsync("DELETE FROM blob_records WHERE expires_at IS NOT NULL AND expires_at <= ?;", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                return await _db.ExecuteScalarAsync<int>("SELECT changes();");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.ExecuteAsync("DELETE FROM blob_records WHERE updated_at < ?;", cutoff.ToUnixTimeSeconds());
                return await _db.ExecuteScalarAsync<int>("SELECT changes();");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task CleanupLocksAsync(CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.ExecuteAsync("DELETE FROM resource_locks WHERE expires_at <= ?;", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<BlobRecord> GetForReadingAsync(string key, CancellationToken ct)
        {
            var lockedBy = Guid.NewGuid().ToString("N");
            await AcquireReadLockAsync(key, lockedBy, ct);

            var lockHeld = true;
            try
            {
                var record = await GetRecordAsync(key, ct);
                if (record == null)
                {
                    lockHeld = false;
                    await ReleaseReadLockAsync(key, lockedBy, ct);
                    throw new KeyNotFoundException($"Blob '{key}' not found.");
                }

                record.LockType = LockType.Read;
                record.OnDisposeAsync = () => UpdateOnReadDisposeAsync(record, lockedBy);
                lockHeld = false;
                return record;
            }
            finally
            {
                if (lockHeld)
                {
                    await ReleaseReadLockAsync(key, lockedBy, ct);
                }
            }
        }

        public async Task<BlobRecord> GetForWritingAsync(string key, CancellationToken ct)
        {
            var lockedBy = Guid.NewGuid().ToString("N");
            await AcquireWriteLockAsync(key, lockedBy, ct);

            var lockHeld = true;
            try
            {
                var record = await GetRecordAsync(key, ct);
                if (record == null)
                {
                    lockHeld = false;
                    await ReleaseWriteLockAsync(key, lockedBy, ct);
                    throw new KeyNotFoundException($"Blob '{key}' not found.");
                }

                record.LockType = LockType.Write;
                record.OnDisposeAsync = () => UpdateOnWriteDisposeAsync(record, lockedBy);
                lockHeld = false;
                return record;
            }
            finally
            {
                if (lockHeld)
                {
                    await ReleaseWriteLockAsync(key, lockedBy, ct);
                }
            }
        }

        public async Task<BlobRecord> GetOrCreateAsync(string key, IBlobStoreOptions options, LockType lockType, CancellationToken ct)
        {
            var writeLockId = Guid.NewGuid().ToString("N");
            await AcquireWriteLockAsync(key, writeLockId, ct);

            var lockHeld = true;
            try
            {
                var now = DateTimeOffset.UtcNow;
                var record = await GetRecordAsync(key, ct);
                var isNew = record == null;

                if (isNew)
                {
                    record = new BlobRecord
                    {
                        Key = key,
                        CreatedAt = now,
                        UpdatedAt = now,
                        AccessedAt = now
                    };

                    ApplyOptions(record, options, now);
                    await InsertRecordAsync(record, ct);
                }
                else
                {
                    ApplyOptions(record, options, now);
                    await UpdateRecordAsync(record, ct);
                }

                var effectiveLock = (!isNew && lockType == LockType.Read) ? LockType.Read : LockType.Write;

                if (effectiveLock == LockType.Read)
                {
                    lockHeld = false;
                    await ReleaseWriteLockAsync(key, writeLockId, ct);

                    var readLockId = Guid.NewGuid().ToString("N");
                    await AcquireReadLockAsync(key, readLockId, ct);
                    record.LockType = LockType.Read;
                    record.OnDisposeAsync = () => UpdateOnReadDisposeAsync(record, readLockId);
                }
                else
                {
                    record.LockType = LockType.Write;
                    record.OnDisposeAsync = () => UpdateOnWriteDisposeAsync(record, writeLockId);
                    lockHeld = false;
                }

                return record;
            }
            finally
            {
                if (lockHeld)
                {
                    await ReleaseWriteLockAsync(key, writeLockId, ct);
                }
            }
        }

        public async Task<IList<string>> QueryAsync(string pattern, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                var sqlPattern = NormalizePattern(pattern);
                var rows = await _db.QueryAsync<BlobRecordTransport>("SELECT key FROM blob_records WHERE key LIKE ?;", sqlPattern);
                var results = new List<string>(rows.Count);
                foreach (var row in rows)
                {
                    results.Add(row.Key);
                }

                return results;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task UpdateOnReadDisposeAsync(BlobRecord record, string lockedBy)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                record.AccessedAt = now;
                if (record.SlidingExpiration.HasValue)
                {
                    record.ExpiresAt = now.Add(record.SlidingExpiration.Value);
                }

                await UpdateRecordAsync(record, CancellationToken.None);
            }
            finally
            {
                await ReleaseReadLockAsync(record.Key, lockedBy, CancellationToken.None);
            }
        }

        private async Task UpdateOnWriteDisposeAsync(BlobRecord record, string lockedBy)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                record.AccessedAt = now;
                record.UpdatedAt = now;
                if (record.SlidingExpiration.HasValue)
                {
                    record.ExpiresAt = now.Add(record.SlidingExpiration.Value);
                }

                await UpdateRecordAsync(record, CancellationToken.None);
            }
            finally
            {
                await ReleaseWriteLockAsync(record.Key, lockedBy, CancellationToken.None);
            }
        }

        private void ApplyOptions(BlobRecord record, IBlobStoreOptions options, DateTimeOffset now)
        {
            if (options == null)
            {
                if (record.SlidingExpiration.HasValue)
                {
                    record.ExpiresAt = now.Add(record.SlidingExpiration.Value);
                }

                return;
            }

            if (!string.IsNullOrEmpty(options.ContentType))
            {
                record.ContentType = options.ContentType;
            }

            if (!string.IsNullOrEmpty(options.Hash))
            {
                record.Hash = options.Hash;
            }

            if (!string.IsNullOrEmpty(options.Metadata))
            {
                record.Metadata = options.Metadata;
            }

            if (options.AbsoluteExpiration.HasValue)
            {
                record.ExpiresAt = options.AbsoluteExpiration.Value;
            }
            else if (options.Ttl.HasValue)
            {
                record.ExpiresAt = now.Add(options.Ttl.Value);
            }
            else if (record.SlidingExpiration.HasValue)
            {
                record.ExpiresAt = now.Add(record.SlidingExpiration.Value);
            }

            if (options.SlidingExpiration.HasValue)
            {
                record.SlidingExpiration = options.SlidingExpiration;
                record.ExpiresAt = now.Add(options.SlidingExpiration.Value);
            }
        }

        private async Task<BlobRecord> GetRecordAsync(string key, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                var t = await _db.Table<BlobRecordTransport>().Where(r => r.Key == key).FirstOrDefaultAsync();
                return t != null ? ToRecord(t) : null;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task InsertRecordAsync(BlobRecord record, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.InsertAsync(ToTransport(record));
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task UpdateRecordAsync(BlobRecord record, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.UpdateAsync(ToTransport(record));
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private static BlobRecord ToRecord(BlobRecordTransport t) => new BlobRecord
        {
            Key = t.Key,
            Metadata = t.Metadata,
            ContentType = t.ContentType,
            Size = t.Size,
            Hash = t.Hash,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(t.CreatedAtUnix),
            UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(t.UpdatedAtUnix),
            AccessedAt = DateTimeOffset.FromUnixTimeSeconds(t.AccessedAtUnix),
            SlidingExpiration = t.SlidingExpirationSeconds.HasValue
                ? TimeSpan.FromSeconds(t.SlidingExpirationSeconds.Value)
                : (TimeSpan?)null,
            ExpiresAt = t.ExpiresAtUnix.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(t.ExpiresAtUnix.Value)
                : (DateTimeOffset?)null,
        };

        private static BlobRecordTransport ToTransport(BlobRecord r) => new BlobRecordTransport
        {
            Key = r.Key,
            Metadata = r.Metadata,
            ContentType = r.ContentType,
            Size = r.Size,
            Hash = r.Hash,
            CreatedAtUnix = r.CreatedAt.ToUnixTimeSeconds(),
            UpdatedAtUnix = r.UpdatedAt.ToUnixTimeSeconds(),
            AccessedAtUnix = r.AccessedAt.ToUnixTimeSeconds(),
            SlidingExpirationSeconds = r.SlidingExpiration.HasValue
                ? (long?)r.SlidingExpiration.Value.TotalSeconds
                : null,
            ExpiresAtUnix = r.ExpiresAt?.ToUnixTimeSeconds(),
        };

        private async Task AcquireReadLockAsync(string resourceId, string lockedBy, CancellationToken ct)
        {
            var deadline = DateTimeOffset.UtcNow + _defaultTimeout;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (await TryAcquireReadLockAsync(resourceId, lockedBy, ct))
                {
                    return;
                }

                if (DateTimeOffset.UtcNow >= deadline)
                {
                    throw new TimeoutException($"Timeout while acquiring read lock for '{resourceId}'.");
                }

                await Task.Delay(100, ct);
            }
        }

        private async Task AcquireWriteLockAsync(string resourceId, string lockedBy, CancellationToken ct)
        {
            var deadline = DateTimeOffset.UtcNow + _defaultTimeout;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (await TryAcquireWriteLockAsync(resourceId, lockedBy, ct))
                {
                    return;
                }

                if (DateTimeOffset.UtcNow >= deadline)
                {
                    throw new TimeoutException($"Timeout while acquiring write lock for '{resourceId}'.");
                }

                await Task.Delay(100, ct);
            }
        }

        private async Task<bool> TryAcquireReadLockAsync(string resourceId, string lockedBy, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var effectiveTimeout = _defaultTimeout < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : _defaultTimeout;
                var expiresAt = (DateTimeOffset.UtcNow + effectiveTimeout).ToUnixTimeSeconds();

                await _db.ExecuteAsync("BEGIN IMMEDIATE;");
                await _db.ExecuteAsync("DELETE FROM resource_locks WHERE expires_at <= ?;", now);
                await _db.ExecuteAsync(
                    "INSERT INTO resource_locks (resource_id, is_write_lock, locked_by, locked_at, expires_at) " +
                    "SELECT ?, 0, ?, ?, ? " +
                    "WHERE NOT EXISTS (" +
                    "    SELECT 1 FROM resource_locks " +
                    "    WHERE resource_id = ? AND is_write_lock = 1 AND expires_at > ?" +
                    ");",
                    resourceId, lockedBy, now, expiresAt, resourceId, now
                );
                var inserted = await _db.ExecuteScalarAsync<int>("SELECT changes();");
                await _db.ExecuteAsync("COMMIT;");
                return inserted > 0;
            }
            catch
            {
                await _db.ExecuteAsync("ROLLBACK;");
                throw;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task<bool> TryAcquireWriteLockAsync(string resourceId, string lockedBy, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var effectiveTimeout = _defaultTimeout < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : _defaultTimeout;
                var expiresAt = (DateTimeOffset.UtcNow + effectiveTimeout).ToUnixTimeSeconds();

                await _db.ExecuteAsync("BEGIN IMMEDIATE;");
                await _db.ExecuteAsync("DELETE FROM resource_locks WHERE expires_at <= ?;", now);
                await _db.ExecuteAsync(
                    "INSERT INTO resource_locks (resource_id, is_write_lock, locked_by, locked_at, expires_at) " +
                    "SELECT ?, 1, ?, ?, ? " +
                    "WHERE NOT EXISTS (" +
                    "    SELECT 1 FROM resource_locks " +
                    "    WHERE resource_id = ? AND expires_at > ?" +
                    ");",
                    resourceId, lockedBy, now, expiresAt, resourceId, now
                );
                var inserted = await _db.ExecuteScalarAsync<int>("SELECT changes();");
                await _db.ExecuteAsync("COMMIT;");
                return inserted > 0;
            }
            catch
            {
                await _db.ExecuteAsync("ROLLBACK;");
                throw;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task ReleaseReadLockAsync(string resourceId, string lockedBy, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.ExecuteAsync(
                    "DELETE FROM resource_locks WHERE resource_id = ? AND locked_by = ? AND is_write_lock = 0;",
                    resourceId, lockedBy
                );
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task ReleaseWriteLockAsync(string resourceId, string lockedBy, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await _db.ExecuteAsync(
                    "DELETE FROM resource_locks WHERE resource_id = ? AND locked_by = ? AND is_write_lock = 1;",
                    resourceId, lockedBy
                );
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task EnsureSchemaAsync()
        {
            await _db.ExecuteAsync(
                "CREATE TABLE IF NOT EXISTS blob_records (" +
                "    key TEXT PRIMARY KEY, " +
                "    metadata TEXT, " +
                "    content_type TEXT, " +
                "    size INTEGER, " +
                "    hash TEXT, " +
                "    created_at INTEGER NOT NULL, " +
                "    updated_at INTEGER NOT NULL, " +
                "    accessed_at INTEGER NOT NULL, " +
                "    sliding_expiration_seconds INTEGER, " +
                "    expires_at INTEGER" +
                ");"
            );

            await _db.ExecuteAsync(
                "CREATE TABLE IF NOT EXISTS resource_locks (" +
                "    id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "    resource_id TEXT NOT NULL, " +
                "    is_write_lock INTEGER NOT NULL DEFAULT 0, " +
                "    locked_by TEXT NOT NULL, " +
                "    locked_at INTEGER NOT NULL, " +
                "    expires_at INTEGER NOT NULL" +
                ");"
            );

            await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_blob_records_expires_at ON blob_records(expires_at);");
            await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_resource_locks_resource_id ON resource_locks(resource_id);");
        }

        private static string NormalizePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return "%";
            }

            return pattern.Replace('*', '%');
        }
    }
}
