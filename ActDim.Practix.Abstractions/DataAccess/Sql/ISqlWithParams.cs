using System;
using System.Linq.Expressions;

namespace ActDim.Practix.Abstractions.DataAccess.Sql
{
    public interface ISqlWithParams<TParams>
    {
        ISqlWithParamsAndTable<TParams, TTable> Table<TTable>();
        ISqlWithParamsAndTable<TParams, TTable1, TTable2> Table<TTable1, TTable2>();
        ISqlWithParamsAndTable<TParams, TTable1, TTable2, TTable3> Table<TTable1, TTable2, TTable3>();

        string Generate(Expression<Func<TParams, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TParams, string>> expression, DbProviderType providerType, params object[] formatArgs);

        IDbOperation CreateOperation(Expression<Func<TParams, string>> expression,
            DbProviderType providerType,
            params object[] formatArgs);
    }
}
