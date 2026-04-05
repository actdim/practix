using OrthoBits.Abstractions.DataAccess;
using System.Data.Common;

namespace OrthoBits.DataAccess
{
    internal class CountDbOperation : CommonDbOperation
    {
        public CountDbOperation(DbProviderType providerType, string sqlCommandText, object parametersObj) :
            base(providerType, sqlCommandText, parametersObj)
        {
        }

        public CountDbOperation(DbProviderType providerType, string sqlCommandText, params DbParameter[] parameters) :
            base(providerType, sqlCommandText, parameters)
        {
        }

        public CountDbOperation(DbProviderType providerType, string sqlCommandText) :
            base(providerType, sqlCommandText)
        {
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            var baseCommand = base.CreateCommand(connection);
            var projector = HelperCaches.GetDialect(connection);
            baseCommand.CommandText = projector.CountQuery(baseCommand.CommandText);
            return baseCommand;
        }
    }
}