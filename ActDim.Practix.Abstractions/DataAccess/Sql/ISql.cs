namespace ActDim.Practix.Abstractions.DataAccess.Sql
{
    public interface ISql
    {
        ISqlWithTable<TTable> Table<TTable>();
        ISqlWithTable<TTable1, TTable2> Table<TTable1, TTable2>();
        ISqlWithTable<TTable1, TTable2, TTable3> Table<TTable1, TTable2, TTable3>();
        ISqlWithParams<TParams> Params<TParams>(TParams paramsObj);
    }
}
