using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.Abstractions.DataAccess.Generic;
using ActDim.Practix.Abstractions.DataAccess.Sql;

namespace ActDim.Practix.Abstractions.DataAccess
{
	/// <summary>
	///
	/// </summary>
	public interface IDbService
	{
        string ConnectionString { get; }

		DbProviderType ProviderType { get; }

        ISql Sql { get; }

		Task<int> ExecuteNonQueryAsync(params IDbOperation[] ops);

		Task<int> ExecuteNonQueryAsync(IDbOperation[] ops, CancellationToken cancellationToken = default);

		// Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation op);
		Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation op, CancellationToken cancellationToken = default);

		// Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation<TEntity> op);
		Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation<TEntity> op, CancellationToken cancellationToken = default);

		// Task<TResult> ExecuteScalarAsync<TResult>(IDbOperation op);
		Task<TResult> ExecuteScalarAsync<TResult>(IDbOperation op, CancellationToken cancellationToken = default);

		Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(string sql, object pars = null);

		Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(string sql, IDbFetcher<TEntity> fetcher, object pars);
	}
}
