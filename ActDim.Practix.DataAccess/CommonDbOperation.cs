using OrthoBits.Abstractions.DataAccess;
using System.Data;
using System.Data.Common;

namespace OrthoBits.DataAccess
{
    public class CommonDbOperationOptions
    {
        public string SqlCommandText { set; get; }
        public List<DbParameter> Parameters { set; get; }
        // public DbProviderType ProviderType { set; get; }

        public CommonDbOperationOptions()
        {
            Parameters = new List<DbParameter>();
        }
    }

    // TODO: add CommandTimeout support
    public class CommonDbOperation : IDbOperation
    {
        public DbProviderType ProviderType
        {
            get;
        }

        public virtual DbCommand CreateCommand(DbConnection connection)
        {
            var command = connection.CreateCommand();
            var projector = HelperCaches.GetDialect(connection.ConnectionString);
            command.CommandText = SqlCommandText;
            if (ParametersObj != null)
            {
                var objType = ParametersObj.GetType();
                var accessor = FastMember.TypeAccessor.Create(objType);

                command.Parameters.AddRange(accessor.GetMembers().Select(op =>
                {
                    var parameter = command.CreateParameter();
                    projector.ProjectParameter(parameter);
                    parameter.ParameterName = op.Name;
                    parameter.Value = projector.ProjectEntityValue(accessor[ParametersObj, op.Name]);
                    return projector.ProjectParameter(parameter);
                }).ToArray());
            }
            else
            {
                command.Parameters.AddRange(Parameters.Select(x => projector.ProjectParameter(x)).ToArray());
            }

            return command;
        }

        public string SqlCommandText
        {
            get;
        }

        public DbParameter[] Parameters
        {
            get;
        }
        = new DbParameter[0];

        public object ParametersObj
        {
            get;
        }

        // internal CommonDbOperation(CommonDbOperationOptions options)
        // {            
        //     SqlCommandText = options.SqlCommandText;
        //     Parameters = options.Parameters.ToArray();
        // }

        internal CommonDbOperation(DbProviderType providerType, CommonDbOperationOptions options)
        {
            ProviderType = providerType;
            SqlCommandText = options.SqlCommandText;
            Parameters = options.Parameters.ToArray();
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, object parametersObj)
        {
            ProviderType = providerType;
            SqlCommandText = sqlCommandText;
            ParametersObj = parametersObj;
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText, params DbParameter[] parameters)
        {
            ProviderType = providerType;
            SqlCommandText = sqlCommandText;
            Parameters = parameters;
        }

        public CommonDbOperation(DbProviderType providerType, string sqlCommandText)
        {
            ProviderType = providerType;
            SqlCommandText = sqlCommandText;
        }
    }
}