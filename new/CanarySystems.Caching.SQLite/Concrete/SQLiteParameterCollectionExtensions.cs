using System;
using System.Data;
using System.Data.SQLite;

namespace CanarySystems.Caching.SQLite
{
    internal static class SQLiteParameterCollectionExtensions
    {
        // For all values where the length is less than the below value, try setting the size of the
        // parameter for better performance.
        public const int DefaultValueColumnWidth = 8000;

        // Maximum size of a primary key column is 900 bytes (898 bytes from the key + 2 additional bytes required by
        // the Sql Server).
        public const int CacheEntryIdColumnWidth = 449;

        public static SQLiteParameterCollection AddCacheEntryId(this SQLiteParameterCollection parameters, string value)
        {
            return parameters.AddWithValue(ColumnNames.Key, DbType.String, CacheEntryIdColumnWidth, value);
        }

        public static SQLiteParameterCollection AddCacheEntryValue(this SQLiteParameterCollection parameters, byte[] value)
        {
            if (value != null && value.Length < DefaultValueColumnWidth)
            {
                return parameters.AddWithValue(
                    ColumnNames.Value,
                    DbType.Binary,
                    DefaultValueColumnWidth,
                    value);
            }
            else
            {
                // do not mention the size
                return parameters.AddWithValue(ColumnNames.Value, DbType.Binary, value);
            }
        }

        public static SQLiteParameterCollection AddSlidingExpirationInSeconds(
            this SQLiteParameterCollection parameters,
            TimeSpan? value)
        {
            if (value.HasValue)
            {
                return parameters.AddWithValue(
                    ColumnNames.SlidingExpirationInSeconds, DbType.Int64, value.Value.TotalSeconds);
            }
            else
            {
                return parameters.AddWithValue(ColumnNames.SlidingExpirationInSeconds, DbType.Int64, DBNull.Value);
            }
        }

        public static SQLiteParameterCollection AddAbsoluteExpiration(
            this SQLiteParameterCollection parameters,
            DateTimeOffset? utcTime)
        {
            if (utcTime.HasValue)
            {
                return parameters.AddWithValue(
                    ColumnNames.AbsoluteExpiration, DbType.Int64, utcTime.Value.ToUnixTimeMilliseconds());
            }
            else
            {
                return parameters.AddWithValue(
                    ColumnNames.AbsoluteExpiration, DbType.Int64, DBNull.Value);
            }
        }

        public static SQLiteParameterCollection AddWithValue(
            this SQLiteParameterCollection parameters,
            string parameterName,
            DbType dbType,

            object value)
        {
            var parameter = new SQLiteParameter(parameterName, dbType);
            parameter.Value = value;
            parameters.Add(parameter);
            return parameters;
        }

        public static SQLiteParameterCollection AddWithValue(
            this SQLiteParameterCollection parameters,
            string parameterName,
            DbType dbType,
            int size,
            object value)
        {
            var parameter = new SQLiteParameter(parameterName, dbType, size);
            parameter.Value = value;
            parameters.Add(parameter);
            return parameters;
        }

        // Since Mono currently does not have support for DateTimeOffset, we convert the time to UtcDateTime.
        // Even though the database column is of type 'datetimeoffset', we can store the UtcDateTime, in which case
        // the zone is set as 00:00. If you look at the below examples, DateTimeOffset.UtcNow
        // and DateTimeOffset.UtcDateTime are almost the same.
        //
        // Examples:
        // DateTimeOffset.Now:          6/29/2015 1:20:40 PM - 07:00
        // DateTimeOffset.UtcNow:       6/29/2015 8:20:40 PM + 00:00
        // DateTimeOffset.UtcDateTime:  6/29/2015 8:20:40 PM

        public static SQLiteParameterCollection AddExpiresAtTime(
            this SQLiteParameterCollection parameters,
            DateTimeOffset utcTime)
        {
            return parameters.AddWithValue(ColumnNames.ExpiresAtTime, DbType.Int64, utcTime.ToUnixTimeMilliseconds());
        }

        public static SQLiteParameterCollection AddAbsoluteExpirationSQLite(
                    this SQLiteParameterCollection parameters,
                    DateTimeOffset? utcTime)
        {
            if (utcTime.HasValue)
            {
                return parameters.AddWithValue(
                    ColumnNames.AbsoluteExpiration, DbType.Int64, utcTime.Value.ToUnixTimeMilliseconds());
            }
            else
            {
                return parameters.AddWithValue(
                ColumnNames.AbsoluteExpiration, DbType.Int64, DBNull.Value);
            }
        }
    }
}
