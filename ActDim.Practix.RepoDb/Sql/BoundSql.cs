using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using RepoDb;

namespace ActDim.Practix.RepoDb.Sql
{
    /// <summary>
    /// Represents an executable SQL command bound to an active database connection.
    /// Encapsulates the resolved SQL statement and provides execution methods forwarding directly to RepoDB.
    /// </summary>
    public sealed class BoundSql
    {
        /// <summary>
        /// Gets the underlying database connection.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Gets the compiled SQL statement text with resolved column names, table aliases, and dialect quotes.
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundSql"/> class.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The resolved SQL string.</param>
        public BoundSql(IDbConnection connection, string sql)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        }

        /// <summary>
        /// Returns the compiled SQL string. Ideal for inspection in the debugger and logging.
        /// </summary>
        /// <returns>The resolved SQL string.</returns>
        public override string ToString() => Sql;

        /// <summary>
        /// Implicitly converts a <see cref="BoundSql"/> to its underlying SQL string.
        /// </summary>
        /// <param name="boundSql">The bound SQL instance.</param>
        public static implicit operator string(BoundSql boundSql)
        {
            ArgumentNullException.ThrowIfNull(boundSql, nameof(boundSql));
            return boundSql.Sql;
        }

        #region Async Execution Methods

        /// <summary>
        /// Executes the query asynchronously and materializes the results into <typeparamref name="TResult"/> objects.
        /// </summary>
        /// <typeparam name="TResult">The target result/DTO type to materialize each row into.</typeparam>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of materialized <typeparamref name="TResult"/> instances.</returns>
        public Task<IEnumerable<TResult>> ExecuteQueryAsync<TResult>(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            return Connection.ExecuteQueryAsync<TResult>(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes a non-query command (e.g. INSERT, UPDATE, DELETE) asynchronously and returns the number of affected rows.
        /// </summary>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteNonQueryAsync(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            return Connection.ExecuteNonQueryAsync(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set.
        /// </summary>
        /// <typeparam name="TResult">The expected scalar type.</typeparam>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scalar result value.</returns>
        public Task<TResult?> ExecuteScalarAsync<TResult>(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            return Connection.ExecuteScalarAsync<TResult>(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes the query asynchronously and returns a <see cref="DbDataReader"/>.
        /// </summary>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A reader for the query results.</returns>
        public Task<IDataReader> ExecuteReaderAsync(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            return Connection.ExecuteReaderAsync(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout,
                cancellationToken: cancellationToken);
        }

        #endregion

        #region Sync Execution Methods

        /// <summary>
        /// Executes the query synchronously and materializes the results into <typeparamref name="TResult"/> objects.
        /// </summary>
        /// <typeparam name="TResult">The target result/DTO type to materialize each row into.</typeparam>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <returns>A collection of materialized <typeparamref name="TResult"/> instances.</returns>
        public IEnumerable<TResult> ExecuteQuery<TResult>(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null)
        {
            return Connection.ExecuteQuery<TResult>(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout);
        }

        /// <summary>
        /// Executes a non-query command synchronously and returns the number of affected rows.
        /// </summary>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <returns>The number of rows affected.</returns>
        public int ExecuteNonQuery(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null)
        {
            return Connection.ExecuteNonQuery(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout);
        }

        /// <summary>
        /// Executes the query synchronously and returns the first column of the first row in the result set.
        /// </summary>
        /// <typeparam name="TResult">The expected scalar type.</typeparam>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <returns>The scalar result value.</returns>
        public TResult? ExecuteScalar<TResult>(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null)
        {
            return Connection.ExecuteScalar<TResult>(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout);
        }

        /// <summary>
        /// Executes the query synchronously and returns a <see cref="DbDataReader"/>.
        /// </summary>
        /// <param name="param">The parameters object to pass to RepoDB.</param>
        /// <param name="transaction">Optional transactional scope.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <returns>A reader for the query results.</returns>
        public IDataReader ExecuteReader(
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null)
        {
            return Connection.ExecuteReader(
                Sql,
                param: param,
                transaction: transaction,
                commandTimeout: commandTimeout);
        }

        #endregion
    }
}
