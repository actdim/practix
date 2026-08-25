using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.RepoDb;
using ActDim.Practix.RepoDb.Extensions;
using Microsoft.Data.Sqlite;
using RepoDb;
using RepoDb.Attributes;

namespace ActDim.BytePath
{
    [Map("blob_records")]
    internal class BlobRecordTransport
    {
        [Primary, Map("blob_key")]
        public string Key { get; set; } = string.Empty;

        [Map("metadata")]
        public string Metadata { get; set; } = string.Empty;

        [Map("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [Map("size")]
        public long? Size { get; set; }

        [Map("hash")]
        public string Hash { get; set; } = string.Empty;

        [Map("created_at")]
        public long CreatedAtUnix { get; set; }

        [Map("updated_at")]
        public long UpdatedAtUnix { get; set; }

        [Map("accessed_at")]
        public long AccessedAtUnix { get; set; }

        [Map("sliding_expiration_seconds")]
        public long? SlidingExpirationSeconds { get; set; }

        [Map("expires_at")]
        public long? ExpiresAtUnix { get; set; }
    }

    public class SQLiteBlobRegistry : IBlobRegistry
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
        private readonly string _connectionString;
        private readonly TimeSpan _defaultTimeout;
        private readonly SemaphoreSlim _dbSemaphore = new(1, 1);

        public SQLiteBlobRegistry(string connectionString) : this(connectionString, DefaultTimeout)
        {
        }

        public SQLiteBlobRegistry(string connectionString, TimeSpan defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", nameof(connectionString));
            }

            _connectionString = NormalizeConnectionString(connectionString);
            _defaultTimeout = defaultTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : defaultTimeout;

            RepoDbBootstrapper.InitializeSqLite();
            EnsureSchemaAsync().GetAwaiter().GetResult();
        }

        private static string NormalizeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;

            var trimmed = connectionString.Trim();
            if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("URI=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return $"Data Source={trimmed}";
        }

        private async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }

        public async Task DeleteLockedAsync(BlobRecord record, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(record, nameof(record));
            if (record.LockType != LockType.Write)
            {
                throw new InvalidOperationException("Deleting a record requires a write lock on it.");
            }

            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                await conn.ExecuteNonQueryAsync("DELETE FROM blob_locks WHERE blob_key = @Key;", new { Key = record.Key }, cancellationToken: ct);
                await conn.ExecuteNonQueryAsync("DELETE FROM blob_records WHERE blob_key = @Key;", new { Key = record.Key }, cancellationToken: ct);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task ForceUnlockAsync(string key, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                await conn.ExecuteNonQueryAsync("DELETE FROM blob_locks WHERE blob_key = @Key;", new { Key = key }, cancellationToken: ct);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<IList<string>> GetExpiredKeysAsync(CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var rows = await conn.ExecuteQueryAsync<BlobRecordTransport>(
                    "SELECT blob_key FROM blob_records " +
                    "WHERE expires_at IS NOT NULL AND expires_at <= @Now " +
                    "  AND NOT EXISTS (" +
                    "    SELECT 1 FROM blob_locks " +
                    "    WHERE blob_locks.blob_key = blob_records.blob_key AND blob_locks.expires_at > @Now" +
                    "  );",
                    new { Now = now }, cancellationToken: ct);
                return ToKeys(rows);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<IList<string>> GetKeysOlderThanAsync(DateTimeOffset cutoff, bool includeLocked, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                var cutoffUnix = cutoff.ToUnixTimeSeconds();
                if (includeLocked)
                {
                    var all = await conn.ExecuteQueryAsync<BlobRecordTransport>(
                        "SELECT blob_key FROM blob_records WHERE updated_at < @Cutoff;",
                        new { Cutoff = cutoffUnix }, cancellationToken: ct);
                    return ToKeys(all);
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var rows = await conn.ExecuteQueryAsync<BlobRecordTransport>(
                    "SELECT blob_key FROM blob_records " +
                    "WHERE updated_at < @Cutoff " +
                    "  AND NOT EXISTS (" +
                    "    SELECT 1 FROM blob_locks " +
                    "    WHERE blob_locks.blob_key = blob_records.blob_key AND blob_locks.expires_at > @Now" +
                    "  );",
                    new { Cutoff = cutoffUnix, Now = now }, cancellationToken: ct);
                return ToKeys(rows);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private static IList<string> ToKeys(IEnumerable<BlobRecordTransport> rows)
        {
            var keys = new List<string>();
            foreach (var row in rows)
            {
                if (row.Key != null)
                {
                    keys.Add(row.Key);
                }
            }
            return keys;
        }

        public async Task CleanupLocksAsync(CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                await conn.ExecuteNonQueryAsync("DELETE FROM blob_locks WHERE expires_at <= @Now;", new { Now = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, cancellationToken: ct);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct)
        {
            return TryGetForReadingAsync(key, _defaultTimeout, ct);
        }

        public async Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            var existing = await GetRecordAsync(key, ct);
            if (existing == null)
            {
                return new BlobResult(BlobErrorCode.KeyNotFound);
            }

            var lockedBy = Guid.NewGuid().ToString("N");
            try
            {
                await AcquireReadLockAsync(key, lockedBy, timeout, ct);
            }
            catch (TimeoutException)
            {
                return new BlobResult(BlobErrorCode.Timeout);
            }

            var lockHeld = true;
            try
            {
                var record = await GetRecordAsync(key, ct);
                if (record == null)
                {
                    lockHeld = false;
                    await ReleaseReadLockAsync(key, lockedBy, ct);
                    return new BlobResult(BlobErrorCode.KeyNotFound);
                }

                record.LockType = LockType.Read;
                record.OnDisposeAsync = () => UpdateOnReadDisposeAsync(record, lockedBy);
                lockHeld = false;
                return new BlobResult(BlobErrorCode.None, record);
            }
            finally
            {
                if (lockHeld)
                {
                    await ReleaseReadLockAsync(key, lockedBy, ct);
                }
            }
        }

        public Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct)
        {
            return TryGetForWritingAsync(key, _defaultTimeout, ct);
        }

        public async Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            var existing = await GetRecordAsync(key, ct);
            if (existing == null)
            {
                return new BlobResult(BlobErrorCode.KeyNotFound);
            }

            var lockedBy = Guid.NewGuid().ToString("N");
            try
            {
                await AcquireWriteLockAsync(key, lockedBy, timeout, ct);
            }
            catch (TimeoutException)
            {
                return new BlobResult(BlobErrorCode.Timeout);
            }

            var lockHeld = true;
            try
            {
                var record = await GetRecordAsync(key, ct);
                if (record == null)
                {
                    lockHeld = false;
                    await ReleaseWriteLockAsync(key, lockedBy, ct);
                    return new BlobResult(BlobErrorCode.KeyNotFound);
                }

                record.LockType = LockType.Write;
                record.OnDisposeAsync = () => UpdateOnWriteDisposeAsync(record, lockedBy);
                lockHeld = false;
                return new BlobResult(BlobErrorCode.None, record);
            }
            finally
            {
                if (lockHeld)
                {
                    await ReleaseWriteLockAsync(key, lockedBy, ct);
                }
            }
        }

        public Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct)
        {
            return TryGetOrSetAsync(key, options, lockType, _defaultTimeout, ct);
        }

        public async Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct)
        {
            var isNew = await EnsureRecordExistsForLockAsync(key, ct);

            var writeLockId = Guid.NewGuid().ToString("N");
            try
            {
                await AcquireWriteLockAsync(key, writeLockId, timeout, ct);
            }
            catch (TimeoutException)
            {
                return new BlobResult(BlobErrorCode.Timeout);
            }

            var lockHeld = true;
            try
            {
                var now = DateTimeOffset.UtcNow;
                var record = await GetRecordAsync(key, ct);

                record.Apply(options, now);
                await UpdateRecordAsync(record, ct);

                var effectiveLock = (!isNew && lockType == LockType.Read) ? LockType.Read : LockType.Write;

                if (effectiveLock == LockType.Read)
                {
                    lockHeld = false;
                    await ReleaseWriteLockAsync(key, writeLockId, ct);

                    var readLockId = Guid.NewGuid().ToString("N");
                    try
                    {
                        await AcquireReadLockAsync(key, readLockId, timeout, ct);
                    }
                    catch (TimeoutException)
                    {
                        return new BlobResult(BlobErrorCode.Timeout);
                    }
                    record.LockType = LockType.Read;
                    record.OnDisposeAsync = () => UpdateOnReadDisposeAsync(record, readLockId);
                }
                else
                {
                    record.LockType = LockType.Write;
                    record.OnDisposeAsync = () => UpdateOnWriteDisposeAsync(record, writeLockId);
                    lockHeld = false;
                }

                return new BlobResult(BlobErrorCode.None, record, isNew);
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
                await using var conn = await CreateOpenConnectionAsync(ct);
                var sqlPattern = pattern.NormalizeSqlPattern();
                var rows = await conn.ExecuteQueryAsync<BlobRecordTransport>("SELECT blob_key FROM blob_records WHERE blob_key LIKE @Pattern;", new { Pattern = sqlPattern }, cancellationToken: ct);
                return ToKeys(rows);
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

        private async Task<BlobRecord?> GetRecordAsync(string key, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                var transport = (await conn.QueryAsync<BlobRecordTransport>(r => r.Key == key, cancellationToken: ct)).FirstOrDefault();
                return transport != null ? ToRecord(transport) : null;
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
                await using var conn = await CreateOpenConnectionAsync(ct);
                await conn.UpdateAsync(ToTransport(record), cancellationToken: ct);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task<bool> EnsureRecordExistsForLockAsync(string key, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                var rows = await conn.ExecuteNonQueryAsync(
                    "INSERT OR IGNORE INTO blob_records (blob_key, created_at, updated_at, accessed_at) VALUES (@Key, @Now, @Now, @Now);",
                    new { Key = key, Now = now }, cancellationToken: ct
                );
                return rows > 0;
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
                : null,
            ExpiresAt = t.ExpiresAtUnix.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(t.ExpiresAtUnix.Value)
                : null,
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
            ExpiresAtUnix = r.ExpiresAt.HasValue ? (long?)((long)Math.Ceiling(r.ExpiresAt.Value.ToUnixTimeMilliseconds() / 1000.0)) : null,
        };

        private TimeSpan NormalizeAcquireTimeout(TimeSpan timeout)
        {
            return timeout < TimeSpan.Zero ? _defaultTimeout : timeout;
        }

        private async Task AcquireReadLockAsync(string resourceId, string lockedBy, TimeSpan timeout, CancellationToken ct)
        {
            var effectiveTimeout = NormalizeAcquireTimeout(timeout);
            var deadline = DateTimeOffset.UtcNow + effectiveTimeout;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (await TryAcquireReadLockAsync(resourceId, lockedBy, effectiveTimeout, ct))
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

        private async Task AcquireWriteLockAsync(string resourceId, string lockedBy, TimeSpan timeout, CancellationToken ct)
        {
            var effectiveTimeout = NormalizeAcquireTimeout(timeout);
            var deadline = DateTimeOffset.UtcNow + effectiveTimeout;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (await TryAcquireWriteLockAsync(resourceId, lockedBy, effectiveTimeout, ct))
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

        private async Task<bool> TryAcquireReadLockAsync(string resourceId, string lockedBy, TimeSpan timeout, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var effectiveTimeout = timeout < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : timeout;
                var expiresAt = (long)Math.Ceiling((DateTimeOffset.UtcNow + effectiveTimeout).ToUnixTimeMilliseconds() / 1000.0);

                await conn.ExecuteNonQueryAsync("BEGIN IMMEDIATE;", cancellationToken: ct);
                try
                {
                    await conn.ExecuteNonQueryAsync("DELETE FROM blob_locks WHERE expires_at <= @Now;", new { Now = now }, cancellationToken: ct);
                    var inserted = await conn.ExecuteNonQueryAsync(
                        "INSERT INTO blob_locks (blob_key, is_write_lock, locked_by, locked_at, expires_at) " +
                        "SELECT @ResourceId, 0, @LockedBy, @Now, @ExpiresAt " +
                        "WHERE NOT EXISTS (" +
                        "    SELECT 1 FROM blob_locks " +
                        "    WHERE blob_key = @ResourceId AND is_write_lock = 1 AND expires_at > @Now" +
                        ") AND EXISTS (" +
                        "    SELECT 1 FROM blob_records WHERE blob_key = @ResourceId" +
                        ");",
                        new { ResourceId = resourceId, LockedBy = lockedBy, Now = now, ExpiresAt = expiresAt }, cancellationToken: ct
                    );
                    await conn.ExecuteNonQueryAsync("COMMIT;", cancellationToken: ct);
                    return inserted > 0;
                }
                catch
                {
                    await conn.ExecuteNonQueryAsync("ROLLBACK;", cancellationToken: ct);
                    throw;
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task<bool> TryAcquireWriteLockAsync(string resourceId, string lockedBy, TimeSpan timeout, CancellationToken ct)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                await using var conn = await CreateOpenConnectionAsync(ct);
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var effectiveTimeout = timeout < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : timeout;
                var expiresAt = (long)Math.Ceiling((DateTimeOffset.UtcNow + effectiveTimeout).ToUnixTimeMilliseconds() / 1000.0);

                await conn.ExecuteNonQueryAsync("BEGIN IMMEDIATE;", cancellationToken: ct);
                try
                {
                    await conn.ExecuteNonQueryAsync("DELETE FROM blob_locks WHERE expires_at <= @Now;", new { Now = now }, cancellationToken: ct);
                    var inserted = await conn.ExecuteNonQueryAsync(
                        "INSERT INTO blob_locks (blob_key, is_write_lock, locked_by, locked_at, expires_at) " +
                        "SELECT @ResourceId, 1, @LockedBy, @Now, @ExpiresAt " +
                        "WHERE NOT EXISTS (" +
                        "    SELECT 1 FROM blob_locks " +
                        "    WHERE blob_key = @ResourceId AND expires_at > @Now" +
                        ") AND EXISTS (" +
                        "    SELECT 1 FROM blob_records WHERE blob_key = @ResourceId" +
                        ");",
                        new { ResourceId = resourceId, LockedBy = lockedBy, Now = now, ExpiresAt = expiresAt }, cancellationToken: ct
                    );
                    await conn.ExecuteNonQueryAsync("COMMIT;", cancellationToken: ct);
                    return inserted > 0;
                }
                catch
                {
                    await conn.ExecuteNonQueryAsync("ROLLBACK;", cancellationToken: ct);
                    throw;
                }
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
                await using var conn = await CreateOpenConnectionAsync(ct);
                await conn.ExecuteNonQueryAsync(
                    "DELETE FROM blob_locks WHERE blob_key = @ResourceId AND locked_by = @LockedBy AND is_write_lock = 0;",
                    new { ResourceId = resourceId, LockedBy = lockedBy }, cancellationToken: ct
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
                await using var conn = await CreateOpenConnectionAsync(ct);
                await conn.ExecuteNonQueryAsync(
                    "DELETE FROM blob_locks WHERE blob_key = @ResourceId AND locked_by = @LockedBy AND is_write_lock = 1;",
                    new { ResourceId = resourceId, LockedBy = lockedBy }, cancellationToken: ct
                );
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task EnsureSchemaAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                await using var conn = await CreateOpenConnectionAsync();
                await conn.ExecuteNonQueryAsync("BEGIN IMMEDIATE;");
                try
                {
                    await conn.ExecuteNonQueryAsync(
                        "CREATE TABLE IF NOT EXISTS blob_records (" +
                        "    blob_key TEXT PRIMARY KEY, " +
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

                    await conn.ExecuteNonQueryAsync(
                        "CREATE TABLE IF NOT EXISTS blob_locks (" +
                        "    blob_key TEXT NOT NULL, " +
                        "    is_write_lock INTEGER NOT NULL DEFAULT 0, " +
                        "    locked_by TEXT NOT NULL, " +
                        "    locked_at INTEGER NOT NULL, " +
                        "    expires_at INTEGER NOT NULL, " +
                        "    FOREIGN KEY(blob_key) REFERENCES blob_records(blob_key) ON DELETE CASCADE" +
                        ");"
                    );

                    await conn.ExecuteNonQueryAsync("CREATE INDEX IF NOT EXISTS idx_blob_records_expires_at ON blob_records(expires_at);");
                    await conn.ExecuteNonQueryAsync("CREATE INDEX IF NOT EXISTS idx_blob_locks_blob_key ON blob_locks(blob_key);");
                    await conn.ExecuteNonQueryAsync("COMMIT;");
                }
                catch
                {
                    await conn.ExecuteNonQueryAsync("ROLLBACK;");
                    throw;
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
    }
}
