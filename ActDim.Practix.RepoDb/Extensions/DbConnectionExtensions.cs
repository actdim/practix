using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using RepoDb;

namespace ActDim.Practix.RepoDb.Extensions
{
    /// <summary>
    /// Extension methods for ADO.NET <see cref="DbConnection"/> and RepoDb operations.
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// Executes an async action inside a transactional scope, automatically committing on success and rolling back on failure.
        /// </summary>
        public static async Task ExecuteInTransactionAsync(this DbConnection connection, Func<DbTransaction, Task> action, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(action, nameof(action));

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            await using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                await action(transaction);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        /// <summary>
        /// Executes an async func in a transactional scope returning a result, committing on success and rolling back on failure.
        /// </summary>
        public static async Task<T> ExecuteInTransactionAsync<T>(this DbConnection connection, Func<DbTransaction, Task<T>> action, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(action, nameof(action));

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            await using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                var result = await action(transaction);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
