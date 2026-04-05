namespace CanarySystems.Caching.SQLite
{
    internal partial class DbOperations
    {
        // private const string GetTableInfoFormat =
        //    "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE " +
        //    "FROM INFORMATION_SCHEMA.TABLES " +
        //    "WHERE TABLE_SCHEMA = '{0}' " +
        //    "AND TABLE_NAME = '{1}'";

        // private const string UpdateCacheEntryFormat =
        // "UPDATE {0} " +
        // "SET ExpiresAtTime = " +
        //     "(CASE " +
        //         "WHEN DATEDIFF(SECOND, @UtcNow, AbsoluteExpiration) <= SlidingExpirationInSeconds " +
        //         "THEN AbsoluteExpiration " +
        //         "ELSE " +
        //         "DATEADD(SECOND, SlidingExpirationInSeconds, @UtcNow) " +
        //     "END) " +
        // "WHERE Id = @Id " +
        // "AND @UtcNow <= ExpiresAtTime " +
        // "AND SlidingExpirationInSeconds IS NOT NULL " +
        // "AND (AbsoluteExpiration IS NULL OR AbsoluteExpiration <> ExpiresAtTime) ;";         

        private const string UpdateCacheEntryFormat = @"UPDATE {0} 
        SET ExpiresAtTime = 
            (CASE 
                WHEN AbsoluteExpiration-@UtcNow <= SlidingExpirationInSeconds*1000 
                THEN AbsoluteExpiration 
                ELSE SlidingExpirationInSeconds*1000+@UtcNow 
            END) 
        WHERE Id = @Id 
        AND @UtcNow <= ExpiresAtTime 
        AND SlidingExpirationInSeconds IS NOT NULL 
        AND (AbsoluteExpiration IS NULL OR AbsoluteExpiration <> ExpiresAtTime);";

        private const string GetCacheEntryFormat = @"SELECT Id, ExpiresAtTime, SlidingExpirationInSeconds, AbsoluteExpiration, Value FROM {0} WHERE Id = @Id AND @UtcNow <= ExpiresAtTime;";

        // private const string SetCacheEntryFormat =
        // "DECLARE @ExpiresAtTime DATETIMEOFFSET; " +
        // "SET @ExpiresAtTime = " +
        // "(CASE " +
        //         "WHEN (@SlidingExpirationInSeconds IS NUll) " +
        //         "THEN @AbsoluteExpiration " +
        //         "ELSE " +
        //         "DATEADD(SECOND, Convert(bigint, @SlidingExpirationInSeconds), @UtcNow) " +
        // "END);" +
        // "UPDATE {0} SET Value = @Value, ExpiresAtTime = @ExpiresAtTime," +
        // "SlidingExpirationInSeconds = @SlidingExpirationInSeconds, AbsoluteExpiration = @AbsoluteExpiration " +
        // "WHERE Id = @Id " +
        // "IF (@@ROWCOUNT = 0) " +
        // "BEGIN " +
        //     "INSERT INTO {0} " +
        //     "(Id, Value, ExpiresAtTime, SlidingExpirationInSeconds, AbsoluteExpiration) " +
        //     "VALUES (@Id, @Value, @ExpiresAtTime, @SlidingExpirationInSeconds, @AbsoluteExpiration); " +
        // "END ";

        private const string SetCacheEntryFormat = @"DELETE FROM {0}  WHERE Id = @Id; 
               INSERT INTO {0} 
               (Id, Value, ExpiresAtTime, SlidingExpirationInSeconds, AbsoluteExpiration) 
               VALUES (@Id, @Value, @ExpiresAtTime, @SlidingExpirationInSeconds, @AbsoluteExpiration);";

        private const string DeleteCacheEntryFormat = "DELETE FROM {0} WHERE Id = @Id";

        public const string DeleteExpiredCacheEntrysFormat = "DELETE FROM {0} WHERE @UtcNow > ExpiresAtTime";

        public string TableInfo { get; }

        public string GetCacheEntryCommandText { get; }

        public string GetCacheEntryWithoutValueCommandText { get; }

        public string SetCacheEntryCommandText { get; }

        public string DeleteCacheEntryCommandText { get; }

        public string DeleteExpiredCacheEntrysCommandText { get; }

        // From EF's SQLiteQuerySqlGenerator
        private string DelimitIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        // private string EscapeLiteral(string literal)
        // {
        //     return literal.Replace("'", "''");
        // }
    }
}
