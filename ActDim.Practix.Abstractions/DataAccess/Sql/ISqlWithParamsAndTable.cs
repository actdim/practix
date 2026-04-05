using System;
using System.Linq.Expressions;

namespace ActDim.Practix.Abstractions.DataAccess.Sql
{
    public interface ISqlWithParamsAndTable<TParams, TTable>
    {
        string Generate(Expression<Func<TParams, TTable, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TParams, TTable, string>> expression, DbProviderType providerType, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TParams, TTable, string>> expression, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TParams, TTable, string>> expression, DbProviderType providerType, params object[] formatArgs);
    }

    public interface ISqlWithParamsAndTable<TParams, TTable1, TTable2>
    {
        string Generate(Expression<Func<TParams, TTable1, TTable2, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TParams, TTable1, TTable2, string>> expression, DbProviderType providerType, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TParams, TTable1, TTable2, string>> expression, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TParams, TTable1, TTable2, string>> expression, DbProviderType providerType, params object[] formatArgs);
    }

    public interface ISqlWithParamsAndTable<TParams, TTable1, TTable2, TTable3>
    {
        string Generate(Expression<Func<TParams, TTable1, TTable2, TTable3, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TParams, TTable1, TTable2, TTable3, string>> expression, DbProviderType providerType, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TParams, TTable1, TTable2, TTable3, string>> expression, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TParams, TTable1, TTable2, TTable3, string>> expression, DbProviderType providerType, params object[] formatArgs);
    }
}