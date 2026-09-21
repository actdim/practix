using System;
using System.Data;
using System.Linq.Expressions;
using ActDim.Practix.RepoDb.Sql;
using RepoDb;

namespace ActDim.Practix.RepoDb.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="IDbConnection"/> for expression-based SQL query definitions and execution.
    /// </summary>
    public static class DbConnectionSqlExtensions
    {
        /// <summary>
        /// Defines an executable SQL statement for a single entity table using C# string interpolation.
        /// Automatically maps properties and tables to RepoDB metadata and database dialect quotes.
        /// </summary>
        public static BoundSql DefineSql<T1>(
            this IDbConnection connection,
            Expression<Func<T1, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));

            var dbSetting = connection.GetDbSetting();
            var sql = SqlExpressionParser.Parse(expr, dbSetting);
            return new BoundSql(connection, sql);
        }

        /// <summary>
        /// Defines an executable SQL statement for two entity tables (e.g. JOINs) using C# string interpolation.
        /// Automatically maps properties, tables, and lambda parameter aliases to RepoDB metadata and dialect quotes.
        /// </summary>
        public static BoundSql DefineSql<T1, T2>(
            this IDbConnection connection,
            Expression<Func<T1, T2, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));

            var dbSetting = connection.GetDbSetting();
            var sql = SqlExpressionParser.Parse(expr, dbSetting);
            return new BoundSql(connection, sql);
        }

        /// <summary>
        /// Defines an executable SQL statement for three entity tables using C# string interpolation.
        /// </summary>
        public static BoundSql DefineSql<T1, T2, T3>(
            this IDbConnection connection,
            Expression<Func<T1, T2, T3, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));

            var dbSetting = connection.GetDbSetting();
            var sql = SqlExpressionParser.Parse(expr, dbSetting);
            return new BoundSql(connection, sql);
        }

        /// <summary>
        /// Defines an executable SQL statement for four entity tables using C# string interpolation.
        /// </summary>
        public static BoundSql DefineSql<T1, T2, T3, T4>(
            this IDbConnection connection,
            Expression<Func<T1, T2, T3, T4, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));

            var dbSetting = connection.GetDbSetting();
            var sql = SqlExpressionParser.Parse(expr, dbSetting);
            return new BoundSql(connection, sql);
        }

        /// <summary>
        /// Binds a standalone <see cref="SqlTemplate"/> to this connection, compiling it with this connection's dialect settings.
        /// </summary>
        public static BoundSql Bind(this IDbConnection connection, SqlTemplate template)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(template, nameof(template));

            return template.Bind(connection);
        }
    }
}
