namespace ActDim.Practix.RepoDb.Sql
{
    /// <summary>
    /// Specifies the SQL dialect used for database identifier quoting and keyword formatting.
    /// </summary>
    public enum SqlDialect
    {
        /// <summary>
        /// Standard SQL identifier quoting using square brackets ([identifier]).
        /// </summary>
        Standard,

        /// <summary>
        /// SQLite dialect using square brackets ([identifier]).
        /// </summary>
        Sqlite,

        /// <summary>
        /// Microsoft SQL Server dialect using square brackets ([identifier]).
        /// </summary>
        SqlServer,

        /// <summary>
        /// PostgreSQL dialect using double quotes ("identifier").
        /// </summary>
        PostgreSql,

        /// <summary>
        /// MySQL / MariaDB dialect using backticks (`identifier`).
        /// </summary>
        MySql
    }
}
