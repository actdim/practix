using System;
using System.Collections.Generic;
using System.Data.Common;

namespace ActDim.Practix.Abstractions.DataAccess
{
    public interface ISqlDialect
    {
        char ParameterNamePrefix { get; }
        string GetColumnName(string propertyName);
        string GetTableName(string typeName);
        object ProjectDbValue(Type targetType, object dbValue);
        object ProjectEntityValue(object entityValue);
        DbParameter ProjectParameter(DbParameter parameter);
        string CountQuery(string sql);
        string PageQuery(string sql, int skip, int take);
        string PrepareIdent(string value);
        // string PrepareLiteral(string value); // TODO
        // IList<string> BuiltInOperators { get; } // TODO: built-in operator list
    }
}
