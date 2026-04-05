using Autofac;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess.Generic;
using OrthoBits.Abstractions.DataAccess.Sql;
using OrthoBits.Abstractions.Json;
using OrthoBits.Abstractions.Mapping;
using OrthoBits.DataAccess.Sql;
using System.Data;
using System.Data.Common;
using System.Transactions;

namespace OrthoBits.DataAccess
{
    /// <summary>
    /// DbServer
    /// </summary>
    internal class DbService : IDbService
    {
        private readonly IMapper _mapper;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILifetimeScope _scope;

        public string ConnectionString { get; private set; }
        public DbProviderType ProviderType { get; private set; }

        public IDbConnectionProvider ConnectionProvider { get; private set; }

        public ISql Sql { get; private set; }

        public DbService(
            string connString,
            IDbConnectionProvider connectionProvider,
            IMapper mapper,
            IJsonSerializer jsonSerializer,
            ILifetimeScope scope)
        {
            ConnectionString = connString;
            ConnectionProvider = connectionProvider;
            ProviderType = connectionProvider.GetProviderType(connString);
            _mapper = mapper;
            _jsonSerializer = jsonSerializer;
            _scope = scope;
            Sql = new SqlGenerator(_jsonSerializer);
            // _scope.Resolve<...>(new NamedParameter("connString", connectionString));
        }

        internal DbProviderType GetProviderType()
        {
            return DbProviderType.PostgreSQL;
        }

        private async Task<DbConnection> OpenConnectionAsync()
        {

            var connection = ConnectionProvider.CreateConnection(ConnectionString);

            try
            {
                if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }
                _scope.Disposer.AddInstanceForDisposal(connection);
                connection.EnlistTransaction(Transaction.Current);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public async Task ClearConnectionPoolAsync()
        {
            await ConnectionProvider.ClearPoolAsync(ConnectionString);
        }

        private async Task<IList<TEntity>> QueryAsync<TEntity>(IDbOperation operation,
            Func<DbDataReader, Task<IList<TEntity>>> mappingDelegate, CancellationToken cancellationToken)
        {
            using (var connection = await OpenConnectionAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var command = operation.CreateCommand(connection))
                {
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        return await mappingDelegate(reader);
                    }
                }
            }
        }

        private void AssertOperationCount(IDbOperation[] operations)
        {
            if (operations == null || operations.Length == 0)
            {
                throw new InvalidOperationException("Expected at least one operation");
            }
        }

        private void AssertTransactionScope()
        {
            if (Transaction.Current == null)
            {
                throw new InvalidOperationException("TransactionScope required for this operation");
            }
        }

        public async Task<int> ExecuteNonQueryAsync(params IDbOperation[] operations)
        {
            return await ExecuteNonQueryAsync(operations, CancellationToken.None);
        }

        public async Task<int> ExecuteNonQueryAsync(IDbOperation[] operations, CancellationToken cancellationToken)
        {
            AssertOperationCount(operations);
            AssertTransactionScope();
            int counter = 0;
            foreach (var op in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var connection = await OpenConnectionAsync())
                {
                    using (var command = op.CreateCommand(connection))
                    {
                        counter += await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
            return counter;
        }

        private static TResult ConvertScalarResult<TResult>(object result)
        {
            if (result == null || result == DBNull.Value)
            {
                return default;
            }

            var type = typeof(TResult);
            var underlyingType = Nullable.GetUnderlyingType(type);
            return (TResult)Convert.ChangeType(result, underlyingType ?? type);
        }

        public async Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation operation)
        {
            return await ExecuteQueryAsync<TEntity>(operation, CancellationToken.None);
        }

        public async Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation operation, CancellationToken cancellationToken)
        {
            return await QueryAsync(operation, async reader =>
                await GetDefaultFetcher<TEntity>().FetchAsync(reader, _mapper, _jsonSerializer, cancellationToken),
                cancellationToken);
        }

        public async Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation<TEntity> operation,
        CancellationToken cancellationToken)
        {
            var fetcher = operation.Fetcher ?? GetDefaultFetcher<TEntity>();
            return await QueryAsync(operation, reader => fetcher.FetchAsync(reader, _mapper, _jsonSerializer, cancellationToken),
                cancellationToken);
        }

        public async Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(IDbOperation<TEntity> operation)
        {
            return await ExecuteQueryAsync(operation, CancellationToken.None);
        }

        public async Task<TResult> ExecuteScalarAsync<TResult>(IDbOperation operation)
        {
            return await ExecuteScalarAsync<TResult>(operation, CancellationToken.None);
        }

        public async Task<TResult> ExecuteScalarAsync<TResult>(IDbOperation operation, CancellationToken cancellationToken)
        {
            using (var connection = await OpenConnectionAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var command = operation.CreateCommand(connection))
                {
                    var result = await command.ExecuteScalarAsync(cancellationToken);
                    return ConvertScalarResult<TResult>(result);
                }
            }
        }

        private IDbFetcher<TEntity> GetDefaultFetcher<TEntity>()
        {
            var type = typeof(TEntity);
            if (IsSinglePrimitive(type))
            {
                // single primitive value
                return new SinglePrimitiveDatabaseFetcher<TEntity>();
            }
            if (type == typeof(object))
            {
                // dynamic
                return new DynamicDbFetcher<TEntity>();
            }

            return new CommonDbFetcher<TEntity>(ProviderType, _scope);
        }

        private bool IsSinglePrimitive(Type type)
        {
            if (type == typeof(string) || type.IsPrimitive)
            {
                return true;
            }
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (underlyingType == typeof(DateTime))
            {
                return true;
            }
            return false;
        }

        public Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(string sql, object pars)
        {
            var operation = new CommonDbOperation(ProviderType, sql, pars);
            return ExecuteQueryAsync<TEntity>(operation);
        }

        public Task<IList<TEntity>> ExecuteQueryAsync<TEntity>(string sql, IDbFetcher<TEntity> fetcher, object pars)
        {
            var operation = new CommonDbOperation<TEntity>(ProviderType, sql, fetcher, pars);
            return ExecuteQueryAsync(operation);
        }

        public Task<TEntity> ExecuteScalarAsync<TEntity>(string sql, object pars)
        {
            var operation = new CommonDbOperation(ProviderType, sql, pars);
            return ExecuteScalarAsync<TEntity>(operation);
        }
    }
}
