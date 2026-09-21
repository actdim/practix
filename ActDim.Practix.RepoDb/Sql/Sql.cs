using System;
using System.Linq.Expressions;
using ActDim.Practix.RepoDb.Sql;

namespace ActDim.Practix.RepoDb
{
    /// <summary>
    /// Static entry point and builder for defining standalone, dialect-aware SQL templates.
    /// </summary>
    public static class SqlBuilder
    {
        /// <summary>
        /// Creates a dialect-scoped builder for defining queries targeting the specified <see cref="SqlDialect"/>.
        /// </summary>
        /// <param name="dialect">The target SQL dialect.</param>
        /// <returns>A <see cref="DialectSqlBuilder"/> configured for the specified dialect.</returns>
        public static DialectSqlBuilder For(SqlDialect dialect) => new(dialect);

        /// <summary>
        /// Defines a SQL template for a single entity table using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1>(Expression<Func<T1, string>> expr)
            => SqlTemplate.Define(expr);

        /// <summary>
        /// Defines a SQL template for two entity tables (e.g. JOINs) using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2>(Expression<Func<T1, T2, string>> expr)
            => SqlTemplate.Define(expr);

        /// <summary>
        /// Defines a SQL template for three entity tables using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3>(Expression<Func<T1, T2, T3, string>> expr)
            => SqlTemplate.Define(expr);

        /// <summary>
        /// Defines a SQL template for four entity tables using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, string>> expr)
            => SqlTemplate.Define(expr);

        /// <summary>
        /// Fluent builder scoped to a specific <see cref="SqlDialect"/>.
        /// </summary>
        public readonly struct DialectSqlBuilder
        {
            /// <summary>
            /// Gets the configured <see cref="SqlDialect"/>.
            /// </summary>
            public SqlDialect Dialect { get; }

            internal DialectSqlBuilder(SqlDialect dialect)
            {
                Dialect = dialect;
            }

            /// <summary>
            /// Defines a SQL template for a single entity table in this dialect.
            /// </summary>
            public SqlTemplate Define<T1>(Expression<Func<T1, string>> expr)
                => SqlTemplate.Define(Dialect, expr);

            /// <summary>
            /// Defines a SQL template for two entity tables in this dialect.
            /// </summary>
            public SqlTemplate Define<T1, T2>(Expression<Func<T1, T2, string>> expr)
                => SqlTemplate.Define(Dialect, expr);

            /// <summary>
            /// Defines a SQL template for three entity tables in this dialect.
            /// </summary>
            public SqlTemplate Define<T1, T2, T3>(Expression<Func<T1, T2, T3, string>> expr)
                => SqlTemplate.Define(Dialect, expr);

            /// <summary>
            /// Defines a SQL template for four entity tables in this dialect.
            /// </summary>
            public SqlTemplate Define<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, string>> expr)
                => SqlTemplate.Define(Dialect, expr);
        }
    }
}
