using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;


namespace CanarySystems.Caching.SQLite
{
    internal partial class DbOperations : IDbOperations
    {
        /// <summary>
        /// Since there is no specific exception type representing a 'duplicate key' error, we are relying on
        /// the following message number which represents the following text in Microsoft SQL Server database.
        ///     "Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'.
        ///     The duplicate key value is %ls."
        /// You can find the list of system messages by executing the following query:
        /// "SELECT * FROM sys.messages WHERE [text] LIKE '%duplicate%'"
        /// </summary>
        private const int DuplicateKeyErrorId = 2627;

        protected const string GetTableSchemaErrorText =
            "Could not retrieve information of table with schema '{0}' and " +
            "name '{1}'. Make sure you have the table setup and try again. " +
            "Connection string: {2}";

        protected string ConnectionString { get; }

        protected string SchemaName { get; }

        protected string TableName { get; }

        protected ISystemClock SystemClock { get; }

        public DbOperations(string connectionString, /*string schemaName,*/ string tableName, ISystemClock systemClock)
        {
            // https://www.sqlite.org/pragma.html			
            // PRAGMA count_changes=0
            // PRAGMA temp_store=0
            ConnectionString = connectionString;
            // var connectionStringBuilder = new SQLiteConnectionStringBuilder(connectionString);
            // connectionStringBuilder.Pooling = true;			
            // ConnectionString = connectionStringBuilder.ConnectionString;

            // SchemaName = schemaName;
            TableName = tableName;
            SystemClock = systemClock;

            //var tableNameWithSchema = string.Format(
            //    "{0}.{1}", DelimitIdentifier(schemaName), DelimitIdentifier(tableName));
            var tableNameWithSchema = string.Format(
               "{0}",/* DelimitIdentifier(schemaName),*/ DelimitIdentifier(tableName));
            // when retrieving an item, we do an UPDATE first and then a SELECT
            GetCacheEntryCommandText = string.Format(UpdateCacheEntryFormat + GetCacheEntryFormat, tableNameWithSchema);
            GetCacheEntryWithoutValueCommandText = string.Format(UpdateCacheEntryFormat, tableNameWithSchema);
            DeleteCacheEntryCommandText = string.Format(DeleteCacheEntryFormat, tableNameWithSchema);
            DeleteExpiredCacheEntrysCommandText = string.Format(DeleteExpiredCacheEntrysFormat, tableNameWithSchema);
            SetCacheEntryCommandText = string.Format(SetCacheEntryFormat, tableNameWithSchema);
            // GetTableInfoCommandText = string.Format(GetTableInfoFormat, /*EscapeLiteral(schemaName),*/ EscapeLiteral(tableName));
        }

        public void DeleteCacheEntry(string key)
        {
            DeleteCacheEntryAsync(key).Wait();
        }

        public async Task DeleteCacheEntryAsync(string key, CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                using (var cmd = new SQLiteCommand(DeleteCacheEntryCommandText, connection))
                {
                    cmd.Parameters.AddCacheEntryId(key);

                    await connection.OpenAsync(token);

                    await cmd.ExecuteNonQueryAsync(token);
                }
            }
        }

        public void RefreshCacheEntry(string key)
        {
            GetCacheEntry(key, includeValue: false);
        }

        public async Task RefreshCacheEntryAsync(string key, CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            await GetCacheEntryAsync(key, includeValue: false, token: token);
        }

        public CacheEntry GetCacheEntry(string key)
        {
            return GetCacheEntry(key, true);
        }

        public async Task<CacheEntry> GetCacheEntryAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return await GetCacheEntryAsync(key, true);
        }

        protected CacheEntry GetCacheEntry(string key, bool includeValue)
        {
            return GetCacheEntryAsync(key, includeValue).Result;
        }

        protected async Task<CacheEntry> GetCacheEntryAsync(string key, bool includeValue = true, CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            var utcNow = SystemClock.UtcNow;

            string query;
            if (includeValue)
            {
                query = GetCacheEntryCommandText;
            }
            else
            {
                query = GetCacheEntryWithoutValueCommandText;
            }

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters
                        .AddCacheEntryId(key)
                        .AddWithValue("UtcNow", DbType.Int64, utcNow.ToUnixTimeMilliseconds());

                    await connection.OpenAsync(token);

                    var reader = await cmd.ExecuteReaderAsync(
                        CommandBehavior.SingleRow | CommandBehavior.SingleResult,
                        token);

                    if (await reader.ReadAsync(token))
                    {
                        var result = new CacheEntry();
                        result.Key = reader.GetString(ColumnIndexes.Key);
                        result.ExpirationTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(reader[ColumnIndexes.ExpiresAtTime].ToString()));
                        // result.ExpirationTime = DateTimeOffset.Parse(reader[Indexes.ExpiresAtTimeIndex].ToString());

                        if (!await reader.IsDBNullAsync(ColumnIndexes.SlidingExpirationInSeconds, token))
                        {
                            result.SlidingExpiration = TimeSpan.FromSeconds(Convert.ToInt64(reader[ColumnIndexes.SlidingExpirationInSeconds].ToString()));
                        }

                        if (!await reader.IsDBNullAsync(ColumnIndexes.AbsoluteExpiration, token))
                        {
                            result.AbsoluteExpiration = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(reader[ColumnIndexes.AbsoluteExpiration].ToString()));
                            // result.absoluteExpiration = DateTimeOffset.Parse(reader[ColumnIndex.AbsoluteExpirationIndex].ToString());
                        }

                        if (includeValue)
                        {
                            result.Value = (byte[])reader[ColumnIndexes.Value];
                        }

                        return result;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public void SetCacheEntry(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetCacheEntryAsync(key, value, options).Wait();
        }

        public async Task SetCacheEntryAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            var utcNow = SystemClock.UtcNow;

            ValidateOptions(options.SlidingExpiration, options.AbsoluteExpiration);

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);

            if (options.SlidingExpiration == null && absoluteExpiration == null)
            {
                return;
            }

            // ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                using (var upsertCmd = new SQLiteCommand(SetCacheEntryCommandText, connection))
                {
                    upsertCmd.Parameters
                        .AddCacheEntryId(key)
                        .AddCacheEntryValue(value)
                        .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                        .AddAbsoluteExpirationSQLite(absoluteExpiration)
                        // .AddWithValue("UtcNow", DbType.Int64, utcNow.ToUnixTimeMilliseconds())
                        .AddExpiresAtTime(options.SlidingExpiration == null ? absoluteExpiration.Value : utcNow.Add(options.SlidingExpiration.Value))
                        ;

                    await connection.OpenAsync(token);

                    try
                    {
                        await upsertCmd.ExecuteNonQueryAsync(token);
                    }
                    catch (SQLiteException ex)
                    {
                        if (CheckIfDuplicateKeyException(ex))
                        {
                            // There is a possibility that multiple requests can try to add the same item to the cache, in
                            // which case we receive a 'duplicate key' exception on the primary key column.
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public void DeleteExpiredCacheEntries()
        {
            var utcNow = SystemClock.UtcNow;

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                using (var cmd = new SQLiteCommand(DeleteExpiredCacheEntrysCommandText, connection))
                {
                    cmd.Parameters.AddWithValue("UtcNow", DbType.Int64, utcNow.ToUnixTimeMilliseconds());

                    connection.Open();

                    cmd.ExecuteNonQuery();
                }
            }
        }

        protected bool CheckIfDuplicateKeyException(SQLiteException ex)
        {
            if (ex.ErrorCode != 0)
            {
                return ex.ErrorCode == DuplicateKeyErrorId;
            }
            return false;
        }

        protected DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
        {
            // calculate absolute expiration
            DateTimeOffset? absoluteExpiration = null;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= utcNow)
                {
                    // throw new InvalidOperationException("The absolute expiration value must be in the future.");
                    return null;
                }

                absoluteExpiration = options.AbsoluteExpiration.Value;
            }
            return absoluteExpiration;
        }

        protected void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
        {
            if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
            {
                throw new InvalidOperationException("Either absolute or sliding expiration needs to be provided.");
            }
        }
    }
}