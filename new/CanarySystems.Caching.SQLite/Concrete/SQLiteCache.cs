using System;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace CanarySystems.Caching.SQLite
{
    /// <summary>
    /// Distributed cache implementation using SQLite database.
    /// </summary>
    public class SQLiteCache : IDistributedCache // SQLiteDistributedCache
    {
        private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DefaultExpiredItemsDeletionInterval = TimeSpan.FromMinutes(1);

        private readonly IDbOperations _dbOperations;
        private readonly ISystemClock _systemClock;
        private readonly TimeSpan _expiredItemsDeletionInterval;
        private DateTimeOffset _lastExpirationScan;
        private readonly TimeSpan _defaultSlidingExpiration;

        public SQLiteCache(IOptions<SQLiteCacheOptions> options)
        {
            var cacheOptions = options.Value;

            if (string.IsNullOrEmpty(cacheOptions.ConnectionString))
            {
                throw new ArgumentException(
                    $"{nameof(SQLiteCacheOptions.ConnectionString)} cannot be empty or null.");
            }
            // if (string.IsNullOrEmpty(cacheOptions.SchemaName))
            // {
            //     throw new ArgumentException(
            //         $"{nameof(SQLiteCacheOptions.SchemaName)} cannot be empty or null.");
            // }
            if (string.IsNullOrEmpty(cacheOptions.TableName))
            {
                throw new ArgumentException(
                    $"{nameof(SQLiteCacheOptions.TableName)} cannot be empty or null.");
            }

            // SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder(cacheOptions.ConnectionString);
            // if(!File.Exists(builder.DataSource)) {
            // 	SQLiteConnection.CreateFile(builder.DataSource);
            // }

            if (cacheOptions.ExpiredItemsDeletionInterval.HasValue &&
                cacheOptions.ExpiredItemsDeletionInterval.Value < MinimumExpiredItemsDeletionInterval)
            {
                throw new ArgumentException(
                    $"{nameof(SQLiteCacheOptions.ExpiredItemsDeletionInterval)} cannot be less the minimum " +
                    $"value of {MinimumExpiredItemsDeletionInterval.TotalMinutes} minutes.");
            }
            if (cacheOptions.DefaultSlidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cacheOptions.DefaultSlidingExpiration),
                    cacheOptions.DefaultSlidingExpiration,
                    "The sliding expiration value must be positive.");
            }

            _systemClock = cacheOptions.SystemClock ?? new SystemClock();
            _expiredItemsDeletionInterval = cacheOptions.ExpiredItemsDeletionInterval ?? DefaultExpiredItemsDeletionInterval;
            _defaultSlidingExpiration = cacheOptions.DefaultSlidingExpiration;

            _dbOperations = new DbOperations(
                cacheOptions.ConnectionString,
                // cacheOptions.SchemaName,
                cacheOptions.TableName,
                _systemClock);

            using (var connection = new SQLiteConnection(cacheOptions.ConnectionString))
            {
                connection.Open();
                var cmdText = "CREATE TABLE IF NOT " +
                  "EXISTS " + cacheOptions.TableName + " (" +
                  "Id TEXT PRIMARY KEY, " +
                  "Value BLOB NOT NULL," +
                  "ExpiresAtTime BIGINT NOT NULL," +
                  "SlidingExpirationInSeconds BIGINT NULL," +
                  "AbsoluteExpiration BIGINT NULL )";

                using (var cmd = new SQLiteCommand(cmdText, connection))
                {
                    // cmd.ExecuteReader();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public byte[] Get(string key)
        {
            return GetAsync(key).Result;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            var entry = await _dbOperations.GetCacheEntryAsync(key, token);

            ScanForExpiredItemsIfRequired();

            return entry == null ? null : entry.Value;
        }

        public void Refresh(string key)
        {
            RefreshAsync(key).Wait();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await _dbOperations.RefreshCacheEntryAsync(key, token);

            ScanForExpiredItemsIfRequired();
        }

        public void Remove(string key)
        {
            RemoveAsync(key).Wait();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await _dbOperations.DeleteCacheEntryAsync(key, token);

            ScanForExpiredItemsIfRequired();
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, options).Wait();
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            token.ThrowIfCancellationRequested();

            GetOptions(ref options);

            await _dbOperations.SetCacheEntryAsync(key, value, options, token);

            ScanForExpiredItemsIfRequired();
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        private void ScanForExpiredItemsIfRequired()
        {
            var utcNow = _systemClock.UtcNow;
            // TODO: Multiple threads could trigger this scan which leads to multiple calls to database.
            if ((utcNow - _lastExpirationScan) > _expiredItemsDeletionInterval)
            {
                _lastExpirationScan = utcNow;

                Task.Run((Action)DeleteExpiredCacheEntrys);
                // DeleteExpiredCacheEntrys();
            }
        }

        private void DeleteExpiredCacheEntrys()
        {
            _dbOperations.DeleteExpiredCacheEntries();
        }

        private void GetOptions(ref DistributedCacheEntryOptions options)
        {
            if (!options.AbsoluteExpiration.HasValue
                && !options.AbsoluteExpirationRelativeToNow.HasValue
                && !options.SlidingExpiration.HasValue)
            {
                options = new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = _defaultSlidingExpiration
                };
            }
        }
    }
}