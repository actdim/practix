using System;
using System.Data;
using System.Linq.Expressions;
using RepoDb;
using RepoDb.Interfaces;

namespace ActDim.Practix.RepoDb.Sql
{
    /// <summary>
    /// Represents a standalone, reusable SQL template defined using C# expressions without requiring an active connection instance.
    /// Can be rendered against specific database settings or bound to an active connection.
    /// </summary>
    public sealed class SqlTemplate
    {
        /// <summary>
        /// Gets the underlying lambda expression defining the SQL statement.
        /// </summary>
        public LambdaExpression Expression { get; }

        /// <summary>
        /// Gets the default <see cref="SqlDialect"/> configured for this template.
        /// </summary>
        public SqlDialect Dialect { get; }

        /// <summary>
        /// Gets the compiled SQL string rendered using the template's default <see cref="Dialect"/>.
        /// </summary>
        public string Sql => ToString();

        private SqlTemplate(LambdaExpression expression, SqlDialect dialect = SqlDialect.Standard)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            Dialect = dialect;
        }

        #region Factory Methods

        /// <summary>
        /// Defines a SQL template for a single entity table using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1>(Expression<Func<T1, string>> expr)
        {
            return Define(SqlDialect.Standard, expr);
        }

        /// <summary>
        /// Defines a SQL template for a single entity table using the specified <see cref="SqlDialect"/>.
        /// </summary>
        public static SqlTemplate Define<T1>(SqlDialect dialect, Expression<Func<T1, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));
            return new SqlTemplate(expr, dialect);
        }

        /// <summary>
        /// Defines a SQL template for two entity tables (e.g. JOINs) using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2>(Expression<Func<T1, T2, string>> expr)
        {
            return Define(SqlDialect.Standard, expr);
        }

        /// <summary>
        /// Defines a SQL template for two entity tables (e.g. JOINs) using the specified <see cref="SqlDialect"/>.
        /// </summary>
        public static SqlTemplate Define<T1, T2>(SqlDialect dialect, Expression<Func<T1, T2, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));
            return new SqlTemplate(expr, dialect);
        }

        /// <summary>
        /// Defines a SQL template for three entity tables using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3>(Expression<Func<T1, T2, string>> expr)
        {
            return Define(SqlDialect.Standard, expr);
        }

        /// <summary>
        /// Defines a SQL template for three entity tables using the specified <see cref="SqlDialect"/>.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3>(SqlDialect dialect, Expression<Func<T1, T2, T3, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));
            return new SqlTemplate(expr, dialect);
        }

        /// <summary>
        /// Defines a SQL template for three entity tables using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3>(Expression<Func<T1, T2, T3, string>> expr)
        {
            return Define(SqlDialect.Standard, expr);
        }

        /// <summary>
        /// Defines a SQL template for four entity tables using standard dialect.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, string>> expr)
        {
            return Define(SqlDialect.Standard, expr);
        }

        /// <summary>
        /// Defines a SQL template for four entity tables using the specified <see cref="SqlDialect"/>.
        /// </summary>
        public static SqlTemplate Define<T1, T2, T3, T4>(SqlDialect dialect, Expression<Func<T1, T2, T3, T4, string>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr, nameof(expr));
            return new SqlTemplate(expr, dialect);
        }

        #endregion

        #region Rendering and Binding

        /// <summary>
        /// Renders the SQL template using the specified <see cref="SqlDialect"/> without requiring driver dependencies.
        /// </summary>
        /// <param name="dialect">The target SQL dialect.</param>
        /// <returns>The rendered SQL query string.</returns>
        public string Render(SqlDialect dialect)
        {
            return Render(DialectDbSetting.For(dialect));
        }

        /// <summary>
        /// Renders the SQL template using the database setting associated with <typeparamref name="TConnection"/>.
        /// </summary>
        /// <typeparam name="TConnection">The target connection type (e.g. SqliteConnection).</typeparam>
        /// <returns>The rendered SQL query string.</returns>
        public string Render<TConnection>() where TConnection : IDbConnection
        {
            var dbSetting = DbSettingMapper.Get<TConnection>();
            return SqlExpressionParser.Parse(Expression, dbSetting);
        }

        /// <summary>
        /// Renders the SQL template using the provided <see cref="IDbSetting"/>.
        /// </summary>
        /// <param name="dbSetting">The database setting, or null for default quoting.</param>
        /// <returns>The rendered SQL query string.</returns>
        public string Render(IDbSetting? dbSetting)
        {
            return SqlExpressionParser.Parse(Expression, dbSetting);
        }

        /// <summary>
        /// Returns the rendered SQL statement using the template's default <see cref="Dialect"/>. Ideal for debugging and inspections.
        /// </summary>
        /// <returns>The rendered SQL string.</returns>
        public override string ToString() => Render(Dialect);

        /// <summary>
        /// Implicitly converts a <see cref="SqlTemplate"/> to its rendered SQL string in default dialect.
        /// </summary>
        /// <param name="template">The template instance.</param>
        public static implicit operator string(SqlTemplate template)
        {
            ArgumentNullException.ThrowIfNull(template, nameof(template));
            return template.ToString();
        }

        /// <summary>
        /// Binds this template to an active <see cref="IDbConnection"/>, resolving database dialect quotes from the connection.
        /// </summary>
        /// <param name="connection">The target database connection.</param>
        /// <returns>An executable <see cref="BoundSql"/> instance.</returns>
        public BoundSql Bind(IDbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            var dbSetting = connection.GetDbSetting();
            var sql = SqlExpressionParser.Parse(Expression, dbSetting);
            return new BoundSql(connection, sql);
        }

        #endregion
    }
}
