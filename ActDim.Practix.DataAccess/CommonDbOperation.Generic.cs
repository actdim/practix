
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess.Generic;
using System.Data.Common;

namespace OrthoBits.DataAccess
{
    internal class CommonDbOperation<T> : CommonDbOperation, IDbOperation<T>
    {
        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, IDbFetcher<T> fetcher, params DbParameter[] parameters)
            : base(providerType, sqlCommandText, parameters)
        {
            Fetcher = fetcher;
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, IDbFetcher<T> fetcher,
            object parametersObj) : base(providerType, sqlCommandText, parametersObj)
        {
            Fetcher = fetcher;
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, IDbFetcher<T> fetcher) : base(providerType, sqlCommandText)
        {
            Fetcher = fetcher;
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, Func<DbDataReader, T> fetcher, params DbParameter[] parameters)
            : base(providerType, sqlCommandText, parameters)
        {
            Fetcher = new LambdaDbFetcher<T>(fetcher);
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, Func<DbDataReader, T> fetcher,
            object parametersObj) : base(providerType, sqlCommandText, parametersObj)
        {
            Fetcher = new LambdaDbFetcher<T>(fetcher);
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, Func<DbDataReader, T> fetcher) : base(providerType, sqlCommandText)
        {
            Fetcher = new LambdaDbFetcher<T>(fetcher);
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, Func<DbDataReader, Task<T>> fetcher, params DbParameter[] parameters)
            : base(providerType, sqlCommandText, parameters)
        {
            Fetcher = new LambdaDbFetcherAsync<T>(fetcher);
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, Func<DbDataReader, Task<T>> fetcher,
            object parametersObj) : base(providerType, sqlCommandText, parametersObj)
        {
            Fetcher = new LambdaDbFetcherAsync<T>(fetcher);
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, Func<DbDataReader, Task<T>> fetcher) : base(providerType, sqlCommandText)
        {
            Fetcher = new LambdaDbFetcherAsync<T>(fetcher);
        }

        public CommonDbOperation(DbProviderType providerType, CommonDbOperationOptions options, IDbFetcher<T> fetcher) : base(providerType, options)
        {
            Fetcher = fetcher;
        }

        public IDbFetcher<T> Fetcher { get; }
    }
}