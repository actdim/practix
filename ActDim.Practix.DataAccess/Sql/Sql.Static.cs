using OrthoBits.Abstractions.DataAccess.Sql;
using System;

namespace OrthoBits.DataAccess.Sql
{
    [Obsolete]
    public static class Sql
    {
        public static ISqlWithTable<TTable> Table<TTable>()
        {
            return new MLSqlGenerator<object, TTable, object, object>();
        }

        public static ISqlWithTable<TTable1, TTable2> Table<TTable1, TTable2>()
        {
            return new MLSqlGenerator<object, TTable1, TTable2, object>();
        }

        public static ISqlWithTable<TTable1, TTable2, TTable3> Table<TTable1, TTable2, TTable3>()
        {
            return new MLSqlGenerator<object, TTable1, TTable2, TTable3>();
        }

        public static ISqlWithParams<TParams> Params<TParams>(TParams paramsObj)
        {
            return new MLSqlGenerator<TParams, object, object, object>(paramsObj);
        }
    }
}