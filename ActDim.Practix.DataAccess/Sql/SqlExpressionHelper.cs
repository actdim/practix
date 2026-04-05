using OrthoBits.DataAccess.Attributes;
using OrthoBits.DataAccess.EntityMapping.Fetch;
using OrthoBits.Abstractions.DataAccess;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OrthoBits.DataAccess.Sql
{
    static class SqlExpressionHelper
    {
        private static readonly string _errorText =
            $"Unsupported expression. Only list/array index operator ([]), constant or string interpolation (String.Format) are supported.\n\n";

        private static readonly string _resultCommonErrorText =
            _errorText +
            $"Example:\n" +
            $"Generate<ResultType>((res) => $\"select {{res.Id}}, {{res.Name}} from my_table\")";

        private static readonly string _closureParamsErrorText =
            $"Only member access / variable / constant closure expressions are supported";

        private static readonly string _paramsCommonErrorText =
            _errorText +
            $"Example:\n" +
            $"Generate((par) => $\"select * from my_table where id = {{par.id}}\", new {{ id = 5 }})";

        private static readonly string _paramResultCommonErrorText =
            _errorText +
            $"Example:\n" +
            $"Generate((res, par) => $\"select {{res.Id}} from my_table where id = {{par.id}}\", new {{ id = 5 }})\n\n";

        // private static readonly string ComplexUnsupportedErrorText = "Nested complex parameters are not supported";

        private static MethodCallExpression TryGetFormatCallExpression(LambdaExpression expression, out string sqlText)
        {
            sqlText = null;
            var body = expression.Body;
            if (body is ConstantExpression _const)
            {
                sqlText = (string)_const.Value;
                return null;
            }
            if (body.NodeType != ExpressionType.Call ||
                !(body is MethodCallExpression formatMethodCall) || formatMethodCall.Type != typeof(string))
            {
                throw new InvalidOperationException(_resultCommonErrorText);
            }
            sqlText = ExtractSqlText(formatMethodCall);
            if (formatMethodCall.Arguments.Count == 1)
            {
                return null;
            }
            return formatMethodCall;
        }

        internal static (string, (string pName, object pValue)[]) GenerateSqlInternal(LambdaExpression expression,
            Type paramsType,
            Type[] tableTypes,
            object paramsObject,
            DbProviderType providerType = default)
        {
            var formatMethodCall = TryGetFormatCallExpression(expression, out var sqlText);
            if (formatMethodCall == null)
            {
                return (sqlText, Array.Empty<(string, object)>());
            }

            ParameterExpression paramsArg = null;
            if (paramsType != null)
            {
                paramsArg = expression.Parameters[0];
            }

            var tableArgs = expression.Parameters.Skip(paramsArg == null ? 0 : 1)
                .Select((p, i) =>
                    new
                    {
                        tableType = tableTypes[i],
                        parameter = p
                    })
                .ToList();

            var argsList = GetArgsList(formatMethodCall);
            var sqlParams = new List<(string, object)>();

            var formatParams = argsList
                .Select((exp, i) =>
                {
                    if (exp == paramsArg)
                    {
                        throw new InvalidOperationException("Only properties supported for parameters");
                    }

                    if (exp.NodeType == ExpressionType.Convert)
                    {
                        exp = (exp as UnaryExpression).Operand;
                        // ?
                    }

                    var ta = tableArgs.FirstOrDefault(x => x.parameter == exp);
                    if (ta != null)
                    {
                        return ExtractTableArgParams(exp, ta.tableType, ta.parameter, providerType);
                    }

                    var srcExp = ExtractSourceExpression(exp);

                    ta = tableArgs.FirstOrDefault(x => x.parameter == srcExp);
                    if ((ta != null || srcExp.Type.GetCustomAttribute(typeof(TableAttribute), true) is TableAttribute)
                        && srcExp != paramsArg)
                    {
                        return ExtractTableArgParams(exp, ta?.tableType ?? srcExp.Type, ta?.parameter, providerType);
                    }

                    if (srcExp.Type == paramsType)
                    {
                        var param = ExtractParamArgParams(exp, paramsObject, providerType, tableTypes.FirstOrDefault() ?? paramsType);
                        return (object)param.Flush(sqlParams);
                    }

                    var closureVal = ExtractClosureExpressionValue(exp);
                    if (closureVal.parameters.Length > 0)
                    {
                        sqlParams.AddRange(closureVal.parameters);
                    }

                    return closureVal.sql;
                })
                .ToArray();

            return (string.Format(sqlText, formatParams), sqlParams.ToArray());
        }

        internal static string GenerateSqlInternal(LambdaExpression expression,
            Type paramsType,
            Type[] tableTypes,
            DbProviderType providerType = default)
        {
            var formatMethodCall = TryGetFormatCallExpression(expression, out var sqlText);
            if (formatMethodCall == null)
            {
                return sqlText;
            }

            ParameterExpression paramsArg = null;
            if (paramsType != null)
            {
                paramsArg = expression.Parameters[0];
            }

            var tableArgs = expression.Parameters.Skip(paramsArg == null ? 0 : 1)
                .Select((p, i) =>
                new
                {
                    tableType = tableTypes[i],
                    parameter = p
                })
                .ToList();

            var argsList = GetArgsList(formatMethodCall);

            var formatParams = argsList
                .Select((exp, i) =>
                {
                    if (exp == paramsArg)
                    {
                        throw new InvalidOperationException("Only properties supported for parameters");
                    }

                    var ta = tableArgs.FirstOrDefault(x => x.parameter == exp);
                    if (ta != null)
                    {
                        return ExtractTableArgParams(exp, ta.tableType, ta.parameter, providerType);
                    }

                    var srcExp = ExtractSourceExpression(exp);

                    ta = tableArgs.FirstOrDefault(x => x.parameter == srcExp);
                    if ((ta != null || srcExp.Type.GetCustomAttribute(typeof(TableAttribute), true) is TableAttribute)
                        && srcExp != paramsArg)
                    {
                        return ExtractTableArgParams(exp, ta?.tableType ?? srcExp.Type, ta?.parameter, providerType);
                    }

                    if (srcExp.Type == paramsType)
                    {
                        return ExtractParamArgParams(exp, providerType, tableTypes.FirstOrDefault() ?? paramsType);
                    }

                    var closureVal = ExtractClosureExpressionValue(exp);

                    return closureVal.sql;
                })
                .ToArray();
            return string.Format(sqlText, formatParams);
        }

        private static Expression ExtractSourceExpression(Expression formatParamExp)
        {
            switch (formatParamExp.NodeType)
            {
                case ExpressionType.Parameter:
                    {
                        return formatParamExp;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)formatParamExp;
                        return ExtractSourceExpression(memberExp.Expression);
                    }
                case ExpressionType.Convert:
                    {
                        var unaryExpression = (UnaryExpression)formatParamExp;
                        switch (unaryExpression.Operand)
                        {
                            case MemberExpression memberExp:
                                {
                                    return ExtractSourceExpression(memberExp.Expression);
                                }
                            case ParameterExpression parameterExp:
                                {                                    
                                    return ExtractSourceExpression(parameterExp);
                                }
                            case ConstantExpression constantExp:
                                {
                                    return ExtractSourceExpression(constantExp);
                                }
                            default:
                                {
                                    throw new InvalidOperationException(_resultCommonErrorText);
                                }
                        }
                    }
                case ExpressionType.Constant:
                    {
                        return formatParamExp;
                    }
            }
            throw new InvalidOperationException(_paramResultCommonErrorText);
        }

        private static List<Expression> GetArgsList(MethodCallExpression formatMethodCall)
        {
            List<Expression> argsList;

            var formatArgs = formatMethodCall.Arguments.Skip(1);
            var secondArg = formatArgs.First();
            if (secondArg is NewArrayExpression array)
            {
                argsList = array.Expressions.ToList();
            }
            else
            {
                argsList = formatArgs.ToList();
            }

            return argsList;
        }

        private static (string sql, (string pName, object pValue)[] parameters) ProcessClosureExpressionValue(object value)
        {
            if (value == null)
            {
                return (string.Empty, Array.Empty<(string pName, object pValue)>());
            }

            if (value is CommonDbOperation op)
            {
                var sql = op.SqlCommandText;
                (string pName, object pValue)[] parameters = Array.Empty<(string pName, object pValue)>();
                if (op.Parameters.Length > 0)
                {
                    parameters = op.Parameters.Select(x => (x.ParameterName, x.Value)).ToArray();
                }
                else if (op.ParametersObj != null)
                {
                    var objType = op.ParametersObj.GetType();
                    var accessor = FastMember.TypeAccessor.Create(objType);
                    parameters = accessor.GetMembers().Select(x => (x.Name, accessor[op.ParametersObj, x.Name]))
                        .ToArray();
                }
                return (sql, parameters);
            }

            return ($"{value}", Array.Empty<(string pName, object pValue)>());
        }

        private static (string sql, (string pName, object pValue)[] parameters) DrillDownClosureExpressionValue(MemberExpression memberExpression)
        {
            if (memberExpression.Expression is ConstantExpression cte)
            {
                if (memberExpression.Member is FieldInfo fi)
                {
                    var value = fi.GetValue(cte.Value);
                    return ProcessClosureExpressionValue(value);
                }
                if (memberExpression.Member is PropertyInfo pi)
                {
                    var value = pi.GetValue(cte.Value);
                    return ProcessClosureExpressionValue(value);
                }
                throw new NotSupportedException(_closureParamsErrorText);
            }

            var getterChain = new Stack<MemberExpression>();
            getterChain.Push(memberExpression);
            ConstantExpression nestedCte = null;

            while (true)
            {
                if (memberExpression.Expression is MemberExpression me)
                {
                    getterChain.Push(me);
                    memberExpression = me;
                    continue;
                }
                nestedCte = memberExpression.Expression as ConstantExpression;
                break;
            }

            if (nestedCte == null)
            {
                throw new NotSupportedException(_closureParamsErrorText);
            }

            object constValue = nestedCte.Value;

            while (getterChain.Count > 0)
            {
                var expression = getterChain.Pop();
                if (expression.Member is FieldInfo fieldInfo)
                {
                    constValue = fieldInfo.GetValue(constValue);
                    continue;
                }
                if (expression.Member is PropertyInfo propertyInfo)
                {   
                    constValue = propertyInfo.GetValue(constValue);

                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        constValue = Convert.ToByte(constValue); 
                    }

                    continue;
                }
                throw new NotSupportedException(_closureParamsErrorText);
            }

            return ProcessClosureExpressionValue(constValue);
        }

        private static (string, Type) DrillDownMemberAccess(MemberExpression memberExpression)
        {
            if (memberExpression.Expression is ParameterExpression)
            {
                return (memberExpression.Member.Name, memberExpression.Type);
            }

            var originalMe = memberExpression;
            var path = new Stack<MemberExpression>();
            path.Push(memberExpression);
            while (true)
            {
                if (memberExpression.Expression is MemberExpression me)
                {
                    path.Push(me);
                    memberExpression = me;
                    continue;
                }
                break;
            }

            var propertyNameBuilder = new StringBuilder();
            while (path.Count > 0)
            {
                var expression = path.Pop();
                propertyNameBuilder.Append(expression.Member.Name);

                if (path.Count == 0)
                {
                    return (propertyNameBuilder.ToString(), expression.Type);
                }
            }

            throw new InvalidOperationException($"Unsupported member expression {originalMe}");
        }


        private static (string name, object value) DrillDownMemberAccess<TParams>(MemberExpression memberExpression, TParams paramsObject)
        {
            if (memberExpression.Expression is ParameterExpression)
            {
                var value = GetPropertyValue(paramsObject, new List<string> { memberExpression.Member.Name });
                return (memberExpression.Member.Name, value);
            }

            var originalMe = memberExpression;
            var path = new Stack<MemberExpression>();
            path.Push(memberExpression);
            while (true)
            {
                if (memberExpression.Expression is MemberExpression me)
                {
                    path.Push(me);
                    memberExpression = me;
                    continue;
                }
                break;
            }

            var propertyPath = new List<string>();
            var propertyNameBuilder = new StringBuilder();
            while (path.Count > 0)
            {
                var member = path.Pop();
                propertyNameBuilder.Append(member.Member.Name);
                propertyPath.Add(member.Member.Name);

                if (path.Count == 0)
                {
                    var value = GetPropertyValue(paramsObject, propertyPath);
                    return (propertyNameBuilder.ToString(), value);
                }
            }

            throw new InvalidOperationException($"Unsupported member expression {originalMe}");
        }

        private static (EntityProperty property, bool isComplex) DrillDownMemberAccess(MemberExpression memberExpression, EntityTable table)
        {
            if (memberExpression.Expression is ParameterExpression)
            {
                return (table.FindProperty(memberExpression.Member.Name), false);
            }

            var originalMe = memberExpression;
            var path = new Stack<MemberExpression>();
            path.Push(memberExpression);
            while (true)
            {
                if (memberExpression.Expression is MemberExpression me)
                {
                    path.Push(me);
                    memberExpression = me;
                    continue;
                }
                break;
            }

            while (path.Count > 0)
            {
                var member = path.Pop();
                var property = table.FindProperty(member.Member.Name);
                if (path.Count == 0)
                {
                    return (property, true);
                }
                table = HelperCaches.GetEntityTable(member.Type, table.ProviderType)
                    .CreateVTable(property.ColumnName);

            }

            throw new InvalidOperationException($"Unsupported member expression {originalMe}");
        }


        private static (string sql, (string pName, object pValue)[] parameters) ExtractClosureExpressionValue(
            Expression formatParamExp)
        {
            switch (formatParamExp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        return DrillDownClosureExpressionValue((MemberExpression)formatParamExp);
                    }
                case ExpressionType.Convert:
                    {
                        var operant = ((UnaryExpression)formatParamExp).Operand;

                        if (operant is ConstantExpression constantExp)
                        {
                            if (constantExp.Type.IsEnum && constantExp.Value != null)
                            {
                                var value = Convert.ToByte(constantExp.Value); 
                                return ($"{value}", Array.Empty<(string pName, object pValue)>());
                            }
                        }

                        if (!(operant is MemberExpression memberExp))
                        {
                            throw new InvalidOperationException(_resultCommonErrorText);
                        }

                        return DrillDownClosureExpressionValue(memberExp);
                    }
                case ExpressionType.Constant:
                    {
                        var constantExpression = (ConstantExpression)formatParamExp;
                        return ($"{constantExpression.Value}", Array.Empty<(string pName, object pValue)>()); //safe ToString

                    }
            }
            throw new NotSupportedException(_closureParamsErrorText);
        }

        private static string ExtractParamArgParams(Expression formatParamExp, DbProviderType providerType,
            Type tableType = null)
        {
            if (providerType == default)
            {
                if (tableType == null)
                {
                    throw new ArgumentNullException(nameof(tableType));
                }
                var entityTable = HelperCaches.GetEntityTable(tableType);
                providerType = entityTable.ProviderType;
            }
            switch (formatParamExp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)formatParamExp;
                        var name = DrillDownMemberAccess(memberExp);
                        var projector = HelperCaches.GetDialect(providerType);
                        return $"{projector.ParameterNamePrefix}{name.Item1}";
                    }
                case ExpressionType.Convert:
                    {
                        if (!(((UnaryExpression)formatParamExp).Operand is MemberExpression memberExp))
                        {
                            throw new InvalidOperationException(_resultCommonErrorText);
                        }
                        var name = DrillDownMemberAccess(memberExp);
                        var projector = HelperCaches.GetDialect(providerType);
                        return $"{projector.ParameterNamePrefix}{name.Item1}";
                    }
                case ExpressionType.Constant:
                    return $"{((ConstantExpression)formatParamExp).Value}"; //safe ToString
            }
            throw new NotSupportedException(_paramsCommonErrorText);
        }

        private static ParamResultBase ExtractParamArgParams(Expression formatParamExp,
            object paramsObject, DbProviderType providerType, Type tableType = null)
        {
            if (providerType == default)
            {
                var entityTable = HelperCaches.GetEntityTable(tableType);
                providerType = entityTable.ProviderType;
            }
            switch (formatParamExp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)formatParamExp;
                        var result = DrillDownMemberAccess(memberExp, paramsObject);
                        var projector = HelperCaches.GetDialect(providerType);
                        if (IsEnumerable(result.value))
                        {
                            return new ParamsCollectionResult(projector.ParameterNamePrefix, result.name, result.value);
                        }
                        return new SingleParamResult(projector.ParameterNamePrefix, result.name, result.value);
                    }
                case ExpressionType.Convert:
                    {
                        var unaryExpression = (UnaryExpression)formatParamExp;
                        switch (unaryExpression.Operand)
                        {
                            case MemberExpression memberExp:
                                {
                                    var result = DrillDownMemberAccess(memberExp, paramsObject);
                                    var projector = HelperCaches.GetDialect(providerType);
                                    if (IsEnumerable(result.value))
                                    {
                                        return new ParamsCollectionResult(projector.ParameterNamePrefix, result.name, result.value);
                                    }
                                    return new SingleParamResult(projector.ParameterNamePrefix, result.name, result.value);
                                }
                            default:
                                {
                                    throw new InvalidOperationException(_resultCommonErrorText);
                                }
                        }
                    }
                case ExpressionType.Constant:
                    return new ConstantSqlParamResult($"{((ConstantExpression)formatParamExp).Value}");
            }
            throw new NotSupportedException(_paramsCommonErrorText);
        }

        private static string ExtractTableArgParams(Expression formatParamExp,
            Type tableType,
            ParameterExpression methodParamExp,
            DbProviderType providerType = default)
        {
            // DbProviderType.GenericSQL
            var entityTable = providerType == default? HelperCaches.GetEntityTable(tableType) :
                HelperCaches.GetEntityTable(tableType, providerType);
            if (methodParamExp == formatParamExp)
            {
                var tableName = entityTable.TableName;
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    var projector = HelperCaches.GetDialect(providerType);
                    tableName = projector.GetTableName(tableType.Name);
                }

                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new InvalidOperationException($"{tableType.FullName} has no TableAttribute or Table name is not set");
                }
                return tableName;
            }

            switch (formatParamExp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)formatParamExp;
                        var property = DrillDownMemberAccess(memberExp, entityTable);
                        if (property.property == null)
                        {
                            return $"{Expression.Lambda(memberExp).Compile().DynamicInvoke()}"; // support "local variable member expression"
                        }
                        return FormatColumnName(property.property, property.isComplex);
                    }
                case ExpressionType.Convert:
                    {
                        var unaryExp = (UnaryExpression)formatParamExp;
                        if (unaryExp.Operand is ParameterExpression paramExp)
                        {
                            return ExtractTableArgParams(paramExp, tableType, methodParamExp, providerType);
                        }
                        if (!(unaryExp.Operand is MemberExpression memberExp))
                        {
                            throw new InvalidOperationException(_resultCommonErrorText);
                        }
                        var property = DrillDownMemberAccess(memberExp, entityTable);
                        return FormatColumnName(property.property, property.isComplex);
                    }

            }
            throw new NotSupportedException(_resultCommonErrorText);

        }

        private static string FormatColumnName(EntityProperty property, bool isComplex)
        {
            return isComplex ? $"\"{property.ColumnName}\"" : property.ColumnName;
        }

        private static string ExtractSqlText(MethodCallExpression formatMethodCallExp)
        {
            var arg1 = formatMethodCallExp.Arguments.First();
            if (arg1 is ConstantExpression constant)
            {
                return (string)constant.Value;
            }
            throw new InvalidOperationException(_resultCommonErrorText);
        }

        private static object GetPropertyValue(object obj, List<string> path)
        {
            if (obj == null)
            {
                return null;
            }
            var type = obj.GetType();
            foreach (var propertyName in path)
            {
                var property = type.GetProperty(propertyName);
                obj = property.GetValue(obj);
                if (obj == null)
                {
                    return null;
                }
                type = obj.GetType();
            }
            return obj;
        }

        private static bool IsEnumerable(object thing)
        {
            return !(thing is string) && thing is IEnumerable;
        }

        abstract class ParamResultBase
        {
            protected readonly char ParameterPrefix;

            protected ParamResultBase(char parameterPrefix)
            {
                ParameterPrefix = parameterPrefix;
            }

            public abstract string Flush(List<(string, object)> targetList);
        }

        class ConstantSqlParamResult : ParamResultBase
        {
            private readonly string _sql;

            public ConstantSqlParamResult(string sql) : base((char)0)
            {
                _sql = sql;
            }

            public override string Flush(List<(string, object)> targetList)
            {
                return _sql;
            }
        }

        class ParamsCollectionResult : ParamResultBase
        {
            private readonly Dictionary<string, object> _collection;

            public ParamsCollectionResult(char parameterPrefix, string paramName, object paramValue) : base(parameterPrefix)
            {
                _collection = new Dictionary<string, object>();
                if (!(paramValue is IEnumerable enumerable) || string.IsNullOrWhiteSpace(paramName))
                {
                    return;
                }

                var counter = 0;
                foreach (var value in enumerable)
                {
                    _collection.Add($"{paramName}{DataAccessConstants.ParamCollectionIndexDelimiter}{counter++}", value);
                }
            }

            public override string Flush(List<(string, object)> targetList)
            {
                if (_collection.Count == 0)
                {
                    return string.Empty;
                }
                var namesList = new List<string>();
                foreach (var tuple in _collection)
                {
                    targetList.Add((tuple.Key, tuple.Value));
                    namesList.Add($"{ParameterPrefix}{tuple.Key}");
                }
                return string.Join(",", namesList);
            }
        }

        class SingleParamResult : ParamResultBase
        {
            private readonly string _paramName;
            private readonly object _paramValue;

            public SingleParamResult(char parameterPrefix, string paramName, object paramValue) : base(parameterPrefix)
            {
                _paramName = paramName;
                _paramValue = paramValue;
            }

            public override string Flush(List<(string, object)> targetList)
            {
                if (string.IsNullOrWhiteSpace(_paramName))
                {
                    return string.Empty;
                }
                targetList.Add((_paramName, _paramValue));
                return $"{ParameterPrefix}{_paramName}";
            }
        }
    }
}
