
using ActDim.Practix.Abstractions.DataAccess;
using ActDim.Practix.Abstractions.DataAccess.Generic;
using System.Data.Common;

namespace ActDim.Practix.DataAccess
{
    internal class PagedDbOperation<T> : CommonDbOperation<T>
    {
        private readonly int skip;
        private readonly int take;

        public PagedDbOperation(DbProviderType providerType, string sqlCommandText, IDbFetcher<T> fetcher, int skip, int take, params DbParameter[] parameters)
            : base(providerType, sqlCommandText, fetcher, parameters)
        {
            this.skip = skip;
            this.take = take;
        }

        public PagedDbOperation(DbProviderType providerType, string sqlCommandText, IDbFetcher<T> fetcher, int skip, int take)
            : base(providerType, sqlCommandText, fetcher)
        {
            this.skip = skip;
            this.take = take;
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            var baseCommand = base.CreateCommand(connection);
            var projector = HelperCaches.GetDialect(connection);
            baseCommand.CommandText = projector.PageQuery(baseCommand.CommandText, skip, take);
            return baseCommand;
        }
    }
}