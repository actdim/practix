using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using ActDim.Practix.TypeAccess.Reflection;
using ActDim.Practix;
using ActDim.Practix.Collections;

namespace ActDim.Practix.TypeAccess.Linq.Dynamic // ActDim.Practix.Dynamic
{    
    public static class DynamicHelper //LinqHelper
    {
        private static readonly ConcurrentDictionary<CompositeKey, Delegate> Cache;

        static DynamicHelper()
        {
            Cache = new ConcurrentDictionary<CompositeKey, Delegate>();
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

        //EvaluateGet
        //resultType/valueType
        public static object EvalGet(object source, string expression, Type resultType, params object[] values)
        {
            // TODO: optionally turn off handling aggregation methods (switching inner-scope to IEnumerable context element/item)
            var sourceType = source.GetType();
            var signature = new CompositeKey([.. values, expression, sourceType, resultType]);

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
            var signature = new CompositeKey([.. values, expression, sourceType, resultType]);

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
                    var propertyType = value?.GetType() ?? property.PropertyType;
                    signatureParameters.Add(property.Name);
                    signatureParameters.Add(propertyType);
                    parameterValues.Add(value);
                    parameterExpressions.Add(Expression.Parameter(propertyType, property.Name));
                }
            }
            var signature = new CompositeKey([.. signatureParameters, .. values, expression, resultType]);
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

            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    parameterValues.Add(pair.Value);
                    var propertyType = pair.Value?.GetType() ?? typeof(object);
                    signatureParameters.Add(pair.Key);
                    signatureParameters.Add(propertyType);
                    parameterExpressions.Add(Expression.Parameter(propertyType, pair.Key));
                }
            }
            var signature =
                new CompositeKey([.. signatureParameters, .. values, expression, resultType]);
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
