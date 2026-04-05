using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using ActDim.Practix.TypeAccess.Reflection;
using ActDim.Practix;

namespace ActDim.Practix.TypeAccess.Linq.Dynamic // ActDim.Practix.Dynamic
{
    //http://www.codeproject.com/Articles/110065/Quickly-Generate-and-Use-Dynamic-Class

    //http://dblinq2007.googlecode.com/svn/trunk/lib/DynamicLinq.cs

    //http://docs.go-mono.com/index.aspx?link=N%3aMono.CSharp
    //http://www.mono-project.com/CsharpRepl
    //http://jint.codeplex.com/releases

    //http://www.wintellect.com/CS/blogs/jlikness/archive/2011/04/28/dynamic-types-to-simplify-property-change-notification-in-silverlight-4-and-5.aspx
    //http://dotnetslackers.com/articles/net/Using-the-DLR-to-build-Expression-Trees.aspx
    //http://blogs.msdn.com/b/csharpfaq/archive/2009/09/14/generating-dynamic-methods-with-expression-trees-in-visual-studio-2010.aspx
    //http://blogs.msdn.com/b/vbteam/archive/2007/08/29/implementing-dynamic-searching-using-linq.aspx

    //http://forums.silverlight.net/forums/t/146541.aspx
    //http://elegantcode.com/2010/05/22/silverlight-databind-to-an-anonymous-targetType-who-knew/
    //http://grahammurray.wordpress.com/2010/05/30/binding-to-anonymous-types-in-silverlight/

    //http://stackoverflow.com/questions/307512/how-do-i-apply-orderby-on-an-iqueryable-using-a-string-column-name-within-a-gene

    //http://kamimucode.com/Home.aspx/C-sharp-Eval/1
    //http://static.springsource.org/spring/docs/3.0.5.RELEASE/reference/expressions.html

    //http://netmatze.wordpress.com/2011/06/05/implementieren-eines-anonymen-interfaces-in-c/

    using Signature = TupleSignature;
    public static class DynamicHelper //LinqHelper
    {
        private static readonly ConcurrentDictionary<Signature, Delegate> Cache;

        static DynamicHelper()
        {
            Cache = new ConcurrentDictionary<Signature, Delegate>();
        }

        // Setter
        // public delegate void GenericSetter(object source, object value);

        // Getter
        // public delegate object GenericGetter(object source);

        // EvaluateGet
        public static object EvalGet(object source, string expression, params object[] values)
        {
            return EvalGet(source, expression, typeof(object), values); //null??
        }

        //(Create/Make)EvalGetter
        //sourceType/itType/thisType/thatType/scopeType/contextType
        //resultType/valueType
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="expression"></param>
        /// <param name="resultType"></param>
        /// <param name="values"></param>
        /// <returns>Getter delegate</returns>
        public static Delegate CreateEvalGetter(Type sourceType, string expression, Type resultType, params object[] values)
        {
            // TODO: turn off handling aggregation methods (switching inner-scope to IEnumerable context element/item)
            return DynamicExpression.ParseLambda(sourceType, resultType, expression, values).Compile();
        }
        //TResult/TValue
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expression"></param>
        /// <param name="values"></param>
        /// <returns>Getter delegate</returns>
        public static Func<TSource, TResult> CreateEvalGetter<TSource, TResult>(string expression, params object[] values)
        {
            // TODO: turn off handling aggregation methods (switching inner-scope to IEnumerable context element/item)
            return DynamicExpression.ParseLambda<TSource, TResult>(expression, values).Compile();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="expression"></param>
        /// <param name="resultType"></param>
        /// <param name="values"></param>
        /// <returns>Getter delegate</returns>
        public static Delegate CreateEvalGetter(ParameterExpression[] parameters, string expression, Type resultType, params object[] values)
        {
            return DynamicExpression.ParseLambda(parameters, resultType, expression, values).Compile();
        }

        private static object[] CreateSignatureParameters(object[] values, params object[] extraValues)
        {
            var length = values.Length;
            var parameters = new object[length + extraValues.Length]; //result
            Array.Copy(values, 0, parameters, extraValues.Length, length);
            Array.Copy(extraValues, parameters, extraValues.Length);
            return parameters;
        }

        //EvaluateGet
        //resultType/valueType
        public static object EvalGet(object source, string expression, Type resultType, params object[] values)
        {
            // TODO: optionally turn off handling aggregation methods (switching inner-scope to IEnumerable context element/item)
            var sourceType = source.GetType();
            var signature = new Signature(CreateSignatureParameters(values, expression, sourceType, resultType));

            var d = Cache.GetOrAdd(signature, s =>
            {
                //this <-> object
                return CreateEvalGetter(new[] { Expression.Parameter(sourceType, string.Empty), Expression.Parameter(sourceType, "this") }, expression, resultType, values);
            });
            return d.DynamicInvoke(source, source);
        }

        //TResult/TValue
        public static TResult EvalGet<TSource, TResult>(TSource source, string expression, params object[] values)
        {
            // TODO: optionally turn off handling aggregation methods (switching inner-scope to IEnumerable context element/item)
            var sourceType = typeof(TSource);
            var resultType = typeof(TResult);
            var signature = new Signature(CreateSignatureParameters(values, expression, sourceType, resultType));

            var d = Cache.GetOrAdd(signature, s =>
            {
                //this <-> object
                //return CreateEvalGetter<TSource, TResult>(expression, values);
                return CreateEvalGetter(new ParameterExpression[] { Expression.Parameter(sourceType, string.Empty), Expression.Parameter(sourceType, "this") }, expression, resultType, values);
            });

            return (TResult)d.DynamicInvoke(source, source);
            //return ((Func<TSource, TResult>)d)(source);
        }

        /// <summary>
        /// Overload to support more than one source/context
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="parameters"></param>
        /// <param name="resultType"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static object EvalGet(string expression, object parameters, Type resultType, params object[] values)
        {
            // TODO: optionally turn off handling aggregation methods (switching inner-scope to IEnumerable context element/item)
            var parameterExpressions = new List<ParameterExpression>();
            var parameterValues = new List<object>();
            var signatureParameters = new List<object>();
            if (parameters != null)
            {
                var type = parameters.GetType(); //sourceType/parametersType
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) //pi
                {
                    //var dynamicProperty = ActDim.Practix.Reflection.Dynamic.DynamicProperty.Create(property);
                    //var value = dynamicProperty.GetValue(parameters);

                    //var value = CreateEvalGetter(new ParameterExpression[] { Expression.Parameter(type, string.Empty) }, property.Name, (Type)null).DynamicInvoke(parameters);
                    var value = EvalGet(property.Name, new Dictionary<string, object>() { { string.Empty, parameters } }, (Type)null);
                    var propertyType = value.GetType(); //real property type (instead property.PropertyType)                
                    signatureParameters.Add(property.Name);
                    signatureParameters.Add(propertyType);
                    parameterValues.Add(value);
                    parameterExpressions.Add(Expression.Parameter(propertyType, property.Name));
                }
            }
            var signature = new Signature(CreateSignatureParameters(CreateSignatureParameters(signatureParameters.ToArray(), values), expression, resultType));
            var d = Cache.GetOrAdd(signature, s =>
            {
                return DynamicExpression.ParseLambda(parameterExpressions.ToArray(), resultType, expression, values).Compile();
            });

            return d.DynamicInvoke(parameterValues.ToArray());
        }

        /// <summary>
        /// Overload to support more than one source/context
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="parameters"></param>
        /// <param name="resultType"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static object EvalGet(string expression, IDictionary<string, object> parameters, Type resultType, params object[] values)
        {
            var parameterExpressions = new List<ParameterExpression>();
            var parameterValues = new List<object>();
            var signatureParameters = new List<object>();

            //var parametersObject = DynamicExpression.CreateObject(parameters); //dynamic
            if (parameters != null)
            {
                foreach (var pair in parameters) //kv/entry/item/element
                {
                    parameterValues.Add(pair.Value);
                    var propertyType = pair.Value.GetType();
                    signatureParameters.Add(pair.Key);
                    signatureParameters.Add(propertyType);
                    parameterExpressions.Add(Expression.Parameter(propertyType, pair.Key));
                }
            }
            var signature =
                new Signature(CreateSignatureParameters(CreateSignatureParameters(signatureParameters.ToArray(), values), expression, resultType));
            var d = Cache.GetOrAdd(signature, s =>
            {
                return DynamicExpression.ParseLambda(parameterExpressions.ToArray(), resultType, expression, values).Compile();
            });

            return d.DynamicInvoke(parameterValues.ToArray());
        }

        // TODO: support "this" keyword, support parameters
        public static object EvalSet(object leftSource, string leftExpression, object value) //EvaluateSet
        {
            var parameters = new[] { Expression.Parameter(leftSource.GetType(), string.Empty) };
            var parser = new ExpressionParser(parameters, leftExpression, null);
            var left = parser.Parse(null);

            var unaryExpression = left as UnaryExpression;
            Expression assign = null;
            try
            {
                //if (left.NodeType == ExpressionType.Convert)
                if (unaryExpression != null)
                {
                    assign = Expression.Assign(left, Expression.Convert(Expression.Constant(value), unaryExpression.Operand.Type));
                }
                else
                {
                    assign = Expression.Assign(left, Expression.Convert(Expression.Constant(value), left.Type));
                }
            }
            catch
            {
                throw new ReflectionException(Res.WriteableExpressionExpected);
            }

            // Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(assign);
            var lambda = Expression.Lambda(assign, parameters);

            return lambda.Compile().DynamicInvoke(leftSource);

        }

        // TODO: support "this" keyword, support parameters
        public static object EvalSet(object leftSource, string leftExpression, object rightSource, string rightExpression) //EvaluateSet
        {
            //var left = DynamicExpression.Parse(null, "@0." + leftExpression, new object[] { leftSource });

            var parameters = new[] { Expression.Parameter(leftSource.GetType(), string.Empty) };
            var parser = new ExpressionParser(parameters, leftExpression, null);
            var left = parser.Parse(null);

            //var left = DynamicExpression.ParseLambda(leftSource.GetType(), null, leftExpression).Body;
            //Type resultType = left.Type;
            //UnaryExpression unaryLeft;
            //unaryLeft = left as UnaryExpression;
            //if (unaryLeft != null) //left.NodeType == ExpressionType.Convert
            //{
            //    left = unaryLeft.Operand;
            //}

            //var right = DynamicExpression.Parse(null, "@0." + rightExpression, new object[] { rightSource });            
            //var right = DynamicExpression.ParseLambda(rightSource.GetType(), resultType, rightExpression).Body;

            LambdaExpression right;
            if (rightSource == null)
            {
                right = DynamicExpression.ParseLambda(typeof(object), null, rightExpression);
            }
            else
            {
                right = DynamicExpression.ParseLambda(rightSource.GetType(), null, rightExpression);
            }

            //parameters = new ParameterExpression[] { Expression.Parameter(rightSource.GetType(), "") };
            //parser = new ExpressionParser(parameters, rightExpression, null);
            //var right = parser.Parse(null);
            //var rightValue = Expression.Lambda(right, parameters).Compile().DynamicInvoke(rightSource);
            var rightValue = right.Compile().DynamicInvoke(rightSource);

            // var assign = Expression.Assign(left, right);
            var assign = Expression.Assign(left, Expression.Constant(rightValue));
            // Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(assign);
            var lambda = Expression.Lambda(assign, parameters);

            return lambda.Compile().DynamicInvoke(leftSource);

        }

    }
}