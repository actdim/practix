using System;
using System.Linq.Expressions;

namespace ActDim.Practix.Abstractions.DataAccess.Sql
{
    public interface ISqlWithTable<TTable>
    {
        ISqlWithParamsAndTable<TParams, TTable> Params<TParams>(TParams pramsObject);
        string Generate(Expression<Func<TTable, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TTable, string>> expression, DbProviderType providerType, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TTable, string>> expression, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TTable, string>> expression, DbProviderType providerType, params object[] formatArgs);
    }

    public interface ISqlWithTable<TTable1, TTable2>
    {
        ISqlWithParamsAndTable<TParams, TTable1, TTable2> Params<TParams>(TParams pramsObject);
        string Generate(Expression<Func<TTable1, TTable2, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TTable1, TTable2, string>> expression, DbProviderType providerType, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TTable1, TTable2, string>> expression, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TTable1, TTable2, string>> expression, DbProviderType providerType, params object[] formatArgs);
    }

    public interface ISqlWithTable<TTable1, TTable2, TTable3>
    {
        ISqlWithParamsAndTable<TParams, TTable1, TTable2, TTable3> Params<TParams>(TParams pramsObject);
        string Generate(Expression<Func<TTable1, TTable2, TTable3, string>> expression, params object[] formatArgs);
        string Generate(Expression<Func<TTable1, TTable2, TTable3, string>> expression, DbProviderType providerType, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TTable1, TTable2, TTable3, string>> expression, params object[] formatArgs);
        IDbOperation CreateOperation(
            Expression<Func<TTable1, TTable2, TTable3, string>> expression, DbProviderType providerType, params object[] formatArgs);
    }
}
