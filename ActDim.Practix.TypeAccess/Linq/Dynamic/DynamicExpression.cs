using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
    public static class DynamicExpression
    {
        public static Expression Parse(Type resultType, string expression, params object[] values)
        {
            var parser = new ExpressionParser(null, expression, values);
            return parser.Parse(resultType);
        }

        // thisType/scopeType/contextType
        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, params object[] values)
        {
            return ParseLambda([Expression.Parameter(itType, string.Empty)], resultType, expression, values);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type resultType, string expression, params object[] values)
        {
            var parser = new ExpressionParser(parameters, expression, values);
            return Expression.Lambda(parser.Parse(resultType), parameters);
        }

        public static Expression<Func<T, S>> ParseLambda<T, S>(string expression, params object[] values)
        {
            return (Expression<Func<T, S>>)ParseLambda(typeof(T), typeof(S), expression, values);
        }
    }
}

// https://github.com/AndreVianna/ExpressionParser
// http://www.codeproject.com/Articles/110065/Quickly-Generate-and-Use-Dynamic-Class
// http://dblinq2007.googlecode.com/svn/trunk/lib/DynamicLinq.cs
// http://docs.go-mono.com/index.aspx?link=N%3aMono.CSharp
// http://www.mono-project.com/CsharpRepl
// http://jint.codeplex.com/releases
// http://dotnetslackers.com/articles/net/Using-the-DLR-to-build-Expression-Trees.aspx
// http://blogs.msdn.com/b/csharpfaq/archive/2009/09/14/generating-dynamic-methods-with-expression-trees-in-visual-studio-2010.aspx
// http://blogs.msdn.com/b/vbteam/archive/2007/08/29/implementing-dynamic-searching-using-linq.aspx
// http://stackoverflow.com/questions/307512/how-do-i-apply-orderby-on-an-iqueryable-using-a-string-column-name-within-a-gene
// http://kamimucode.com/Home.aspx/C-sharp-Eval/1
// http://static.springsource.org/spring/docs/3.0.5.RELEASE/reference/expressions.html
// http://netmatze.wordpress.com/2011/06/05/implementieren-eines-anonymen-interfaces-in-c/
