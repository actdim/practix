namespace CanarySystems.Caching.SQLite
{
    public static class ColumnNames
    {
        public const string Key = "Id";
        public const string Value = "Value";
        public const string ExpiresAtTime = "ExpiresAtTime";
        public const string SlidingExpirationInSeconds = "SlidingExpirationInSeconds";
        public const string AbsoluteExpiration = "AbsoluteExpiration";
    }

    public static class ColumnIndexes
    {
        // The value of the following index positions is dependent on how the SQL queries
        // are selecting the columns.
        public const int Key = 0;
        public const int ExpiresAtTime = 1;
        public const int SlidingExpirationInSeconds = 2;
        public const int AbsoluteExpiration = 3;
        public const int Value = 4;
    }
}
