using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using RepoDb;
using RepoDb.Interfaces;

namespace ActDim.Practix.RepoDb.Sql
{
    /// <summary>
    /// Translates C# lambda expressions into SQL statements by resolving entity properties and tables
    /// to their corresponding RepoDB mapped names, table aliases, and dialect-specific identifier quotes.
    /// </summary>
    public static class SqlExpressionParser
    {
        private static readonly Regex PlaceholderRegex = new(
            @"(?<!\{)\{(\d+)(?:,(-?\d+))?(?::([^}]+))?\}(?!\})",
            RegexOptions.Compiled);

        private static readonly ConcurrentDictionary<string, Func<IDbSetting?, string>> TemplateCache = new();

        /// <summary>
        /// Parses a lambda expression into a SQL string using the provided database settings.
        /// </summary>
        /// <param name="expression">The lambda expression representing the SQL query.</param>
        /// <param name="dbSetting">The database setting providing quoting characters for the target database.</param>
        /// <returns>The resolved SQL query string.</returns>
        public static string Parse(LambdaExpression expression, IDbSetting? dbSetting = null)
        {
            ArgumentNullException.ThrowIfNull(expression, nameof(expression));

            var cacheKey = expression.ToString();
            var compiledTemplate = TemplateCache.GetOrAdd(cacheKey, _ => CompileTemplate(expression));
            return compiledTemplate(dbSetting);
        }

        private static Func<IDbSetting?, string> CompileTemplate(LambdaExpression lambda)
        {
            var parameters = lambda.Parameters.ToArray();
            var tokens = ParseNode(lambda.Body, parameters);

            return dbSetting =>
            {
                var sb = new System.Text.StringBuilder();
                foreach (var token in tokens)
                {
                    sb.Append(token.Render(dbSetting));
                }

                return sb.ToString();
            };
        }

        private static List<ISqlToken> ParseNode(Expression node, ParameterExpression[] parameters)
        {
            var tokens = new List<ISqlToken>();

            // Handle string concatenation: expr1 + expr2
            if (node is BinaryExpression binary && binary.NodeType == ExpressionType.Add && binary.Type == typeof(string))
            {
                tokens.AddRange(ParseNode(binary.Left, parameters));
                tokens.AddRange(ParseNode(binary.Right, parameters));
                return tokens;
            }

            // Handle method call to string.Format(...)
            if (node is MethodCallExpression methodCall && methodCall.Method.DeclaringType == typeof(string) && methodCall.Method.Name == nameof(string.Format))
            {
                tokens.AddRange(ParseFormatCall(methodCall, parameters));
                return tokens;
            }

            // Handle constant string
            if (node is ConstantExpression constant && constant.Value is string constStr)
            {
                tokens.Add(new LiteralToken(constStr));
                return tokens;
            }

            // Fallback for evaluated expressions
            var evaluated = Expression.Lambda(node).Compile().DynamicInvoke();
            tokens.Add(new LiteralToken(evaluated?.ToString() ?? string.Empty));
            return tokens;
        }

        private static List<ISqlToken> ParseFormatCall(MethodCallExpression methodCall, ParameterExpression[] parameters)
        {
            var formatArg = methodCall.Arguments[0];
            string formatTemplate;
            if (formatArg is ConstantExpression constExpr && constExpr.Value is string s)
            {
                formatTemplate = s;
            }
            else
            {
                var evaluated = Expression.Lambda(formatArg).Compile().DynamicInvoke();
                formatTemplate = evaluated?.ToString() ?? string.Empty;
            }

            var rawArguments = ExtractFormatArguments(methodCall);
            var resolvedArgs = new ISqlToken[rawArguments.Length];

            for (var i = 0; i < rawArguments.Length; i++)
            {
                resolvedArgs[i] = ResolveArgument(rawArguments[i], parameters);
            }

            var tokens = new List<ISqlToken>();
            var lastIndex = 0;

            foreach (Match match in PlaceholderRegex.Matches(formatTemplate))
            {
                if (match.Index > lastIndex)
                {
                    var textSegment = formatTemplate.Substring(lastIndex, match.Index - lastIndex);
                    tokens.Add(new LiteralToken(UnescapeBraces(textSegment)));
                }

                var argIndex = int.Parse(match.Groups[1].Value);
                if (argIndex >= 0 && argIndex < resolvedArgs.Length)
                {
                    tokens.Add(resolvedArgs[argIndex]);
                }
                else
                {
                    tokens.Add(new LiteralToken(match.Value));
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < formatTemplate.Length)
            {
                var trailingSegment = formatTemplate.Substring(lastIndex);
                tokens.Add(new LiteralToken(UnescapeBraces(trailingSegment)));
            }

            return tokens;
        }

        private static Expression[] ExtractFormatArguments(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count == 2 && methodCall.Arguments[1] is NewArrayExpression newArray && newArray.NodeType == ExpressionType.NewArrayInit)
            {
                return newArray.Expressions.ToArray();
            }

            var list = new List<Expression>();
            for (var i = 1; i < methodCall.Arguments.Count; i++)
            {
                list.Add(methodCall.Arguments[i]);
            }

            return list.ToArray();
        }

        private static ISqlToken ResolveArgument(Expression argument, ParameterExpression[] parameters)
        {
            var unwrapped = UnwrapExpression(argument);
            var useAlias = parameters.Length > 1;

            // Case 1: MemberExpression (e.g. c.Id)
            if (unwrapped is MemberExpression memberExpr && memberExpr.Member is PropertyInfo propInfo)
            {
                var param = parameters.FirstOrDefault(p => p == memberExpr.Expression);
                if (param != null)
                {
                    var alias = useAlias && param.Name != "_" ? param.Name : null;
                    return new ColumnToken(param.Type, propInfo, alias);
                }
            }

            // Case 2: ParameterExpression (e.g. {c})
            if (unwrapped is ParameterExpression paramExpr)
            {
                var param = parameters.FirstOrDefault(p => p == paramExpr);
                if (param != null)
                {
                    var alias = useAlias && param.Name != "_" ? param.Name : null;
                    return new TableToken(param.Type, alias);
                }
            }

            // Case 3: Constant or arbitrary evaluated expression (e.g. local variable or constant)
            try
            {
                var val = Expression.Lambda(argument).Compile().DynamicInvoke();
                return new LiteralToken(val?.ToString() ?? string.Empty);
            }
            catch
            {
                return new LiteralToken(argument.ToString());
            }
        }

        private static Expression UnwrapExpression(Expression expr)
        {
            while (expr is UnaryExpression unary && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            {
                expr = unary.Operand;
            }

            return expr;
        }

        private static string UnescapeBraces(string text)
        {
            return text.Replace("{{", "{").Replace("}}", "}");
        }

        private interface ISqlToken
        {
            string Render(IDbSetting? dbSetting);
        }

        private sealed class LiteralToken : ISqlToken
        {
            private readonly string _text;

            public LiteralToken(string text)
            {
                _text = text;
            }

            public string Render(IDbSetting? dbSetting) => _text;
        }

        private sealed class ColumnToken : ISqlToken
        {
            private readonly Type _entityType;
            private readonly PropertyInfo _propertyInfo;
            private readonly string? _tableAlias;

            public ColumnToken(Type entityType, PropertyInfo propertyInfo, string? tableAlias)
            {
                _entityType = entityType;
                _propertyInfo = propertyInfo;
                _tableAlias = tableAlias;
            }

            public string Render(IDbSetting? dbSetting)
            {
                var classProperty = PropertyCache.Get(_entityType)?.FirstOrDefault(p => p.PropertyInfo == _propertyInfo);
                var mappedColumn = classProperty?.GetMappedName() ?? _propertyInfo.Name;
                var openQuote = dbSetting?.OpeningQuote ?? "[";
                var closeQuote = dbSetting?.ClosingQuote ?? "]";

                var quotedCol = $"{openQuote}{mappedColumn}{closeQuote}";

                if (!string.IsNullOrEmpty(_tableAlias))
                {
                    var quotedAlias = $"{openQuote}{_tableAlias}{closeQuote}";
                    return $"{quotedAlias}.{quotedCol}";
                }

                return quotedCol;
            }
        }

        private sealed class TableToken : ISqlToken
        {
            private readonly Type _entityType;
            private readonly string? _tableAlias;

            public TableToken(Type entityType, string? tableAlias)
            {
                _entityType = entityType;
                _tableAlias = tableAlias;
            }

            public string Render(IDbSetting? dbSetting)
            {
                var mappedTable = ClassMappedNameCache.Get(_entityType) ?? _entityType.Name;
                var openQuote = dbSetting?.OpeningQuote ?? "[";
                var closeQuote = dbSetting?.ClosingQuote ?? "]";

                var quotedTable = $"{openQuote}{mappedTable}{closeQuote}";

                if (!string.IsNullOrEmpty(_tableAlias))
                {
                    var quotedAlias = $"{openQuote}{_tableAlias}{closeQuote}";
                    return $"{quotedTable} AS {quotedAlias}";
                }

                return quotedTable;
            }
        }
    }
}
