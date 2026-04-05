
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using OrthoBits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess.Sql;
using OrthoBits.Abstractions.Json;

namespace OrthoBits.DataAccess.Sql
{
    public class SqlGenerator : ISql
    {
        internal readonly IJsonSerializer JsonSerializer;
        public SqlGenerator(IJsonSerializer jsonSerializer)
        {
            JsonSerializer = jsonSerializer;
        }

        public ISqlWithTable<TTable> Table<TTable>()
        {
            return new MLSqlGenerator<object, TTable, object, object>(JsonSerializer);
        }

        public ISqlWithTable<TTable1, TTable2> Table<TTable1, TTable2>()
        {
            return new MLSqlGenerator<object, TTable1, TTable2, object>(JsonSerializer);
        }

        public ISqlWithTable<TTable1, TTable2, TTable3> Table<TTable1, TTable2, TTable3>()
        {
            return new MLSqlGenerator<object, TTable1, TTable2, TTable3>(JsonSerializer);
        }

        public ISqlWithParams<TParams> Params<TParams>(TParams paramsObj)
        {
            return new MLSqlGenerator<TParams, object, object, object>(JsonSerializer, paramsObj);
        }
    }

    internal class MLSqlGenerator<TP, TT1, TT2, TT3> : SqlGenerator, ISqlWithParams<TP>, ISqlWithTable<TT1>, ISqlWithTable<TT1, TT2>, ISqlWithTable<TT1, TT2, TT3>, ISqlWithParamsAndTable<TP, TT1>, ISqlWithParamsAndTable<TP, TT1, TT2>, ISqlWithParamsAndTable<TP, TT1, TT2, TT3>
    {
        private readonly string _sort;
        private readonly TP _paramsObject;

        internal MLSqlGenerator(IJsonSerializer jsonSerializer, TP paramsObject) : base(jsonSerializer)
        {
            _paramsObject = paramsObject;
        }

        internal MLSqlGenerator(IJsonSerializer jsonSerializer, string sort, TP paramsObject) : base(jsonSerializer)
        {
            _sort = sort;
            _paramsObject = paramsObject;
        }

        internal MLSqlGenerator(IJsonSerializer jsonSerializer, string sort) : base(jsonSerializer)
        {
            _sort = sort;
        }

        internal MLSqlGenerator(IJsonSerializer jsonSerializer) : base(jsonSerializer)
        {
        }

        [Obsolete]
        internal MLSqlGenerator(TP paramsObject) : base(null)
        {
            _paramsObject = paramsObject;
        }

        [Obsolete]
        internal MLSqlGenerator(string sort, TP paramsObject) : base(null)
        {
            _sort = sort;
            _paramsObject = paramsObject;
        }

        [Obsolete]
        internal MLSqlGenerator(string sort) : base(null)
        {
            _sort = sort;
        }

        [Obsolete]
        internal MLSqlGenerator() : base(null)
        {
        }

        private string BuildSql(string sql, object[] formatArgs)
        {
            var builder = new StringBuilder();
            builder.Append(sql);
            if (!string.IsNullOrWhiteSpace(this._sort))
            {
                builder.Append(' ');
                builder.Append(_sort);
            }

            var result = builder.ToString();
            return formatArgs.Length > 0 ? string.Format(result, formatArgs) : result;
        }

        private CommonDbOperationOptions GenerateOptions(LambdaExpression expression,
            Type paramsType,
            Type[] tableTypes,
            DbProviderType providerType = default,
            params object[] formatArgs)
        {
            var tableType = typeof(TT1);
            if (providerType == default)
            {
                providerType = HelperCaches.GetEntityTable(tableType).ProviderType;
            }

            var result = SqlExpressionHelper.GenerateSqlInternal(expression, paramsType, tableTypes, _paramsObject, providerType);
            var sql = result.Item1;
            var sqlParams = new List<DbParameter>();
            foreach (var tuple in result.Item2)
            {
                if (sqlParams.Any(x => x.ParameterName == tuple.pName))
                {
                    continue;
                }
                sqlParams.Add(new DbParam()
                {
                    ParameterName = tuple.pName,
                    Value = tuple.pValue
                });
            }

            return new CommonDbOperationOptions()
            {
                SqlCommandText = BuildSql(sql, formatArgs),
                Parameters = sqlParams
            };
        }

        private CommonDbOperationOptions GenerateOptions(LambdaExpression expression, DbProviderType providerType, Type paramsType, params object[] formatArgs)
        {
            var result = SqlExpressionHelper.GenerateSqlInternal(expression, paramsType, new Type[0], _paramsObject, providerType);
            var sql = result.Item1;
            var @params = result.Item2.Select(x => new DbParam()
            {
                ParameterName = x.pName,
                Value = x.pValue
            }).ToList<DbParameter>();
            return new CommonDbOperationOptions()
            {
                SqlCommandText = BuildSql(sql, formatArgs),
                Parameters = @params
            };
        }

        public new ISqlWithParamsAndTable<TP, TTable> Table<TTable>()
        {
            return new MLSqlGenerator<TP, TTable, TT2, TT3>(JsonSerializer, _sort, _paramsObject);
        }

        public new ISqlWithParamsAndTable<TP, TTable1, TTable2> Table<TTable1, TTable2>()
        {
            return new MLSqlGenerator<TP, TTable1, TTable2, TT3>(JsonSerializer, _sort, _paramsObject);
        }

        public new ISqlWithParamsAndTable<TP, TTable1, TTable2, TTable3> Table<TTable1, TTable2, TTable3>()
        {
            return new MLSqlGenerator<TP, TTable1, TTable2, TTable3>(JsonSerializer, _sort, _paramsObject);
        }

        ISqlWithParamsAndTable<TParams, TT1, TT2, TT3> ISqlWithTable<TT1, TT2, TT3>.Params<TParams>(TParams paramsObject)
        {
            return new MLSqlGenerator<TParams, TT1, TT2, TT3>(JsonSerializer, _sort, paramsObject);
        }

        ISqlWithParamsAndTable<TParams, TT1, TT2> ISqlWithTable<TT1, TT2>.Params<TParams>(TParams paramsObject)
        {
            return new MLSqlGenerator<TParams, TT1, TT2, TT3>(JsonSerializer, paramsObject);
        }

        ISqlWithParamsAndTable<TParams, TT1> ISqlWithTable<TT1>.Params<TParams>(TParams paramsObject)
        {
            return new MLSqlGenerator<TParams, TT1, TT2, TT3>(JsonSerializer, _sort, paramsObject);
        }

        public string Generate(Expression<Func<TP, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new Type[0]), formatArgs);
        }

        public string Generate(Expression<Func<TP, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new Type[0], providerType), formatArgs);
        }

        public string Generate(Expression<Func<TT1, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, null, new[] { typeof(TT1) }), formatArgs);
        }

        public string Generate(Expression<Func<TT1, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, null, new[] { typeof(TT1) }, providerType), formatArgs);
        }

        public string Generate(Expression<Func<TT1, TT2, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, null, new[] { typeof(TT1), typeof(TT2) }), formatArgs);
        }

        public string Generate(Expression<Func<TT1, TT2, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, null, new[] { typeof(TT1), typeof(TT2) }, providerType), formatArgs);
        }

        public string Generate(Expression<Func<TT1, TT2, TT3, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, null, new[] { typeof(TT1), typeof(TT2), typeof(TT3) }), formatArgs);
        }

        public string Generate(Expression<Func<TT1, TT2, TT3, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, null, new[] { typeof(TT1), typeof(TT2), typeof(TT3) }, providerType), formatArgs);
        }

        public string Generate(Expression<Func<TP, TT1, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new[] { typeof(TT1) }), formatArgs);
        }

        public string Generate(Expression<Func<TP, TT1, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new[] { typeof(TT1) }, providerType), formatArgs);
        }

        public string Generate(Expression<Func<TP, TT1, TT2, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2) }), formatArgs);
        }

        public string Generate(Expression<Func<TP, TT1, TT2, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2) }, providerType), formatArgs);
        }

        public string Generate(Expression<Func<TP, TT1, TT2, TT3, string>> expression, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2), typeof(TT3) }), formatArgs);
        }

        public string Generate(Expression<Func<TP, TT1, TT2, TT3, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            return BuildSql(SqlExpressionHelper.GenerateSqlInternal(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2), typeof(TT3) }, providerType), formatArgs);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, providerType, typeof(TP), formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TT1, string>> expression, params object[] formatArgs)
        {
            var providerType = HelperCaches.GetEntityTable(typeof(TT1)).ProviderType;
            var options = GenerateOptions(expression, null, new[] { typeof(TT1) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TT1, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, null, new[] { typeof(TT1) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TT1, TT2, string>> expression, params object[] formatArgs)
        {
            var providerType = HelperCaches.GetEntityTable(typeof(TT1)).ProviderType;
            var options = GenerateOptions(expression, null, new[] { typeof(TT1), typeof(TT2) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TT1, TT2, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, null, new[] { typeof(TT1), typeof(TT2) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TT1, TT2, TT3, string>> expression, params object[] formatArgs)
        {
            var providerType = HelperCaches.GetEntityTable(typeof(TT1)).ProviderType;
            var options = GenerateOptions(expression, null, new[] { typeof(TT1), typeof(TT2), typeof(TT3) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TT1, TT2, TT3, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, null, new[] { typeof(TT1), typeof(TT2), typeof(TT3) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, TT1, string>> expression, params object[] formatArgs)
        {
            var providerType = HelperCaches.GetEntityTable(typeof(TT1)).ProviderType;
            var options = GenerateOptions(expression, typeof(TP), new[] { typeof(TT1) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, TT1, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, typeof(TP), new[] { typeof(TT1) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, TT1, TT2, string>> expression, params object[] formatArgs)
        {
            var providerType = HelperCaches.GetEntityTable(typeof(TT1)).ProviderType;
            var options = GenerateOptions(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, TT1, TT2, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, TT1, TT2, TT3, string>> expression, params object[] formatArgs)
        {
            var providerType = HelperCaches.GetEntityTable(typeof(TT1)).ProviderType;
            var options = GenerateOptions(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2), typeof(TT3) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }

        public IDbOperation CreateOperation(Expression<Func<TP, TT1, TT2, TT3, string>> expression, DbProviderType providerType, params object[] formatArgs)
        {
            var options = GenerateOptions(expression, typeof(TP), new[] { typeof(TT1), typeof(TT2), typeof(TT3) }, providerType, formatArgs);
            return new CommonDbOperation(providerType, options);
        }
    }
}