using ActDim.Practix.Collections.Concurrent;
using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Reflectron
{
    public static partial class Reflectron
    {
        private static readonly ConcurrentFactoryDictionary<MethodInfo, FastMethodCallDelegate> MethodCallerCache =
            new ConcurrentFactoryDictionary<MethodInfo, FastMethodCallDelegate>(CreateMethodCaller);

        private static readonly Func<(MethodInfo, Type), Delegate> GetTypedMethodCallerDelegate = GetTypedMethodCaller;
        private static readonly ConcurrentFactoryDictionary<(MethodInfo, Type), Delegate> TypedMethodCallerCache =
            new ConcurrentFactoryDictionary<(MethodInfo, Type), Delegate>(GetTypedMethodCallerDelegate);

        /// <summary>
        /// Gets a fast untyped delegate to call the specified method.
        /// </summary>
        /// <param name="method">The method to invoke.</param>
        /// <returns>A <see cref="FastMethodCallDelegate"/> for dynamic invocation.</returns>
        public static FastMethodCallDelegate GetMethodCaller(MethodInfo method)
        {
            return MethodCallerCache.GetOrCreateValue(method);
        }

        /// <summary>
        /// Creates a typed method call delegate for the specified method info.
        /// </summary>
        /// <typeparam name="T">The delegate type.</typeparam>
        /// <param name="method">The method to invoke.</param>
        /// <returns>A compiled method invoker delegate.</returns>
        public static T GetMethodCaller<T>(MethodInfo method)
        {
            object result = GetMethodCaller(method, typeof(T));
            return (T)result;
        }

        /// <summary>
        /// Creates a method call delegate for the specified method info and delegate type.
        /// </summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="delegateType">The delegate type matching the method signature.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static Delegate GetMethodCaller(MethodInfo method, Type delegateType)
        {
            Guard.Against.Null(method, nameof(method));

            if (!delegateType.IsSubclassOf(typeof(Delegate)))
            {
                throw new ArgumentException($"{nameof(delegateType)} is not a Delegate.", nameof(delegateType));
            }

            var pair = (method, delegateType);
            return TypedMethodCallerCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a static method caller delegate for the specified method name on <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static TDelegate GetStaticMethodCaller<T, TDelegate>(string name)
        {
            return GetStaticMethodCaller<TDelegate>(typeof(T), name);
        }

        /// <summary>
        /// Gets a static method caller delegate for the specified method name on the given type.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="type">The declaring type.</param>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static TDelegate GetStaticMethodCaller<TDelegate>(Type type, string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var delegateType = typeof(TDelegate);
            if (!delegateType.IsSubclassOf(BaseDelegateType))
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(TDelegate));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            var invokeParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var methodInfo = type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                invokeParamTypes,
                new ParameterModifier[0]);

            return GetMethodCaller<TDelegate>(methodInfo);
        }

        /// <summary>
        /// Gets an instance method caller delegate for the specified method name on <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The declaring instance type.</typeparam>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static TDelegate GetMethodCaller<T, TDelegate>(string name)
        {
            return GetMethodCaller<TDelegate>(typeof(T), name);
        }

        /// <summary>
        /// Gets an instance method caller delegate for the specified method name on the given type.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="type">The declaring instance type.</param>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static TDelegate GetMethodCaller<TDelegate>(Type type, string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var delegateType = typeof(TDelegate);
            if (!delegateType.IsSubclassOf(BaseDelegateType))
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(TDelegate));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            var delegateParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToList();
            if (delegateParamTypes[0] != type)
            {
                throw new ArgumentException($"Invalid delegate type ({delegateType.FullName}). First parameter should be of type {type.FullName} (to represent the instance).", nameof(TDelegate));
            }
            delegateParamTypes.RemoveAt(0);

            var methodInfo = type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [.. delegateParamTypes],
                new ParameterModifier[0]);

            return GetMethodCaller<TDelegate>(methodInfo);
        }

        /// <summary>
        /// Wraps an untyped delegate into a fast dynamic invocation delegate.
        /// </summary>
        /// <param name="realDelegate">The target delegate.</param>
        /// <returns>A <see cref="FastDynamicDelegate"/> wrapper.</returns>
        public static FastDynamicDelegate GetFastDynamicDelegate(Delegate realDelegate)
        {
            var result = GetMethodCaller(realDelegate.Method);
            return (parameters) => result(realDelegate.Target, parameters);
        }

        private static FastMethodCallDelegate CreateMethodCaller(MethodInfo methodInfo)
        {
            var paramExpr = Expression.Parameter(typeof(object[]));
            var targetExpr = Expression.Parameter(ObjectType);
            Expression castTarget = null;

            if (!methodInfo.IsStatic)
            {
                castTarget = targetExpr;
                if (methodInfo.DeclaringType != ObjectType)
                {
                    castTarget = Expression.Convert(targetExpr, methodInfo.DeclaringType);
                }
            }

            var varExprs = new List<ParameterExpression>();
            var beforeInstrExprs = new List<Expression>();
            var afterInstrExprs = new List<Expression>();

            Expression[] accessorExprs = null;
            var parameters = methodInfo.GetParameters();
            var count = parameters.Length;
            if (count != 0)
            {
                accessorExprs = new Expression[count];
                for (int i = 0; i < count; i++)
                {
                    var parameter = parameters[i];
                    var paramType = parameter.ParameterType;

                    var constExpr = Expression.Constant(i);
                    Expression accessParamExpr = Expression.ArrayAccess(paramExpr, constExpr);

                    if (paramType.IsByRef)
                    {
                        paramType = paramType.GetElementType();

                        if (paramType != ObjectType)
                        {
                            var varExpr = Expression.Variable(paramType);
                            varExprs.Add(varExpr);
                            accessorExprs[i] = varExpr;

                            if (!parameter.IsOut)
                            {
                                var effectiveAccessParamExpr = accessParamExpr;
                                if (paramType != ObjectType)
                                {
                                    effectiveAccessParamExpr = Expression.Convert(accessParamExpr, paramType);
                                }
                                var setInExpr = Expression.Assign(varExpr, effectiveAccessParamExpr);
                                beforeInstrExprs.Add(setInExpr);
                            }

                            Expression accessVarExpr = varExpr;
                            if (paramType != ObjectType)
                            {
                                accessVarExpr = Expression.Convert(varExpr, ObjectType);
                            }

                            var setOutExpr = Expression.Assign(accessParamExpr, accessVarExpr);
                            afterInstrExprs.Add(setOutExpr);
                            continue;
                        }
                    }

                    if (paramType != ObjectType)
                    {
                        accessParamExpr = Expression.Convert(accessParamExpr, paramType);
                    }

                    accessorExprs[i] = accessParamExpr;
                }
            }

            MethodCallExpression callExpr;
            if (methodInfo.IsStatic)
            {
                callExpr = Expression.Call(methodInfo, accessorExprs);
            }
            else
            {
                callExpr = Expression.Call(castTarget, methodInfo, accessorExprs);
            }

            var instrExprs = new List<Expression>();
            instrExprs.AddRange(beforeInstrExprs);

            ParameterExpression resultVarExpr = null;
            Expression bodyExpr = callExpr;
            if (methodInfo.ReturnType != VoidType)
            {
                if (methodInfo.ReturnType != ObjectType)
                {
                    bodyExpr = Expression.Convert(callExpr, ObjectType);
                }
                resultVarExpr = Expression.Variable(ObjectType);
                varExprs.Add(resultVarExpr);
                bodyExpr = Expression.Assign(resultVarExpr, bodyExpr);
            }

            instrExprs.Add(bodyExpr);
            instrExprs.AddRange(afterInstrExprs);

            if (methodInfo.ReturnType == VoidType)
            {
                instrExprs.Add(Expression.Constant(null, ObjectType));
            }
            else
            {
                instrExprs.Add(resultVarExpr);
            }

            bodyExpr = Expression.Block(ObjectType, varExprs, instrExprs);

            var result = Expression.Lambda<FastMethodCallDelegate>(bodyExpr, targetExpr, paramExpr);
            return result.Compile();
        }

        private static Delegate GetTypedMethodCaller((MethodInfo, Type) pair)
        {
            var method = pair.Item1;
            var delegateType = pair.Item2;

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            if (invokeMethodInfo == null)
            {
                throw new InvalidOperationException("The given delegate type does not have an Invoke method.");
            }

            var methodReturnType = method.ReturnType;
            var invokeReturnType = invokeMethodInfo.ReturnType;

            bool isMethodVoid = methodReturnType == VoidType;
            bool isInvokeVoid = invokeReturnType == VoidType;
            if (isMethodVoid != isInvokeVoid)
            {
                throw new InvalidOperationException("The return type of the method and the delegate is incompatible.");
            }

            var invokeParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var methodParamTypes = new List<Type>();
            if (!method.IsStatic)
            {
                methodParamTypes.Add(method.DeclaringType);
            }

            methodParamTypes.AddRange(method.GetParameters().Select(pi => pi.ParameterType));

            var count = invokeParamTypes.Length;
            if (methodParamTypes.Count != count)
            {
                throw new InvalidOperationException("The number of parameters between the method and the delegate is not compatible. Note that non-static methods have the additional \"this\" parameter as the first one.");
            }

            var paramExprs = new ParameterExpression[count];

            var startIndex = 0;
            var argCount = count;
            if (!method.IsStatic)
            {
                startIndex = 1;
                argCount--;
            }

            var argExprs = new Expression[argCount];
            for (var i = 0; i < argCount; i++)
            {
                var argument = GetArgumentExpression(i + startIndex, methodParamTypes, invokeParamTypes, paramExprs);
                argExprs[i] = argument;
            }

            MethodCallExpression callExpr;
            if (method.IsStatic)
            {
                callExpr = Expression.Call(method, argExprs);
            }
            else
            {
                var instanceExpr = GetArgumentExpression(0, methodParamTypes, invokeParamTypes, paramExprs);
                callExpr = Expression.Call(instanceExpr, method, argExprs);
            }

            Expression resultExpr = callExpr;
            if (methodReturnType != invokeReturnType)
            {
                resultExpr = Expression.Convert(resultExpr, invokeReturnType);
            }

            var lambdaExpr = Expression.Lambda(delegateType, resultExpr, paramExprs);
            return lambdaExpr.Compile();
        }
    }
}
