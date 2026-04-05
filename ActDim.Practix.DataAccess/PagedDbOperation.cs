
using OrthoBits.Abstractions.DataAccess;
using System.Data.Common;

namespace OrthoBits.DataAccess
{
    internal class PagedDbOperation : CommonDbOperation
    {
        private readonly int skip;
        private readonly int take;

        public PagedDbOperation(DbProviderType providerType, string sqlCommandText, object parametersObj, int skip, int take) : base(providerType, sqlCommandText, parametersObj)
        {
            this.skip = skip;
            this.take = take;
        }

        public PagedDbOperation(DbProviderType providerType, string sqlCommandText, DbParameter[] parameters, int skip, int take) : base(providerType, sqlCommandText, parameters)
        {
            this.skip = skip;
            this.take = take;
        }

        public PagedDbOperation(DbProviderType providerType, string sqlCommandText, int skip, int take) : base(providerType, sqlCommandText)
        {
            this.skip = skip;
            this.take = take;
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            var baseCommand = base.CreateCommand(connection);
            var projector = HelperCaches.GetDialect(ProviderType);
            baseCommand.CommandText = projector.PageQuery(baseCommand.CommandText, skip, take);
            return baseCommand;
        }
    }
}