using ActDim.Practix.Collections.Concurrent;
using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ActDim.Reflectron
{
    public static partial class Reflectron
    {
        private static readonly Func<(ConstructorInfo, Type), Delegate> GetTypedConstructorDelegate = GetConstructorInternal;
        private static readonly ConcurrentFactoryDictionary<(ConstructorInfo, Type), Delegate> TypedConstructorCache =
            new ConcurrentFactoryDictionary<(ConstructorInfo, Type), Delegate>(GetTypedConstructorDelegate);

        private static readonly Func<ConstructorInfo, FastDynamicDelegate> GetConstructorDelegate = CreateConstructorEx;
        private static readonly ConcurrentFactoryDictionary<ConstructorInfo, FastDynamicDelegate> ConstructorCache =
            new ConcurrentFactoryDictionary<ConstructorInfo, FastDynamicDelegate>(GetConstructorDelegate);

        /// <summary>
        /// Tries to get a default constructor. Returns null if the type does not have a public default constructor.
        /// </summary>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <returns>A default constructor delegate, or null.</returns>
        public static Func<T> TryGetDefaultConstructorDelegate<T>()
        {
            var type = typeof(T);
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                return null;
            }

            return GetDefaultConstructor<T>();
        }

        /// <summary>
        /// Creates a fast dynamic constructor delegate for the specified delegate signature type.
        /// </summary>
        /// <param name="delegateType">The delegate type whose return type and parameter types match the constructor.</param>
        /// <returns>A fast dynamic constructor delegate.</returns>
        public static FastDynamicDelegate CreateConstructorEx(Type delegateType)
        {
            var ctorInfo = GetConstructorInfo(delegateType);
            return GetConstructorEx(ctorInfo);
        }

        /// <summary>
        /// Creates a compiled constructor delegate matching <typeparamref name="TConstructorDelegate"/>.
        /// </summary>
        /// <typeparam name="TConstructorDelegate">The delegate type.</typeparam>
        /// <returns>A compiled constructor delegate.</returns>
        public static TConstructorDelegate CreateConstructor<TConstructorDelegate>() where TConstructorDelegate : Delegate
        {
            var ctorDelegateType = typeof(TConstructorDelegate);
            var ctorInfo = GetConstructorInfo(ctorDelegateType);
            return GetConstructor<TConstructorDelegate>(ctorInfo);
        }

        /// <summary>
        /// Gets the <see cref="ConstructorInfo"/> matching the given delegate type.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <returns>The matching <see cref="ConstructorInfo"/>.</returns>
        public static ConstructorInfo GetConstructorInfo<TDelegate>()
        {
            return GetConstructorInfo(typeof(TDelegate));
        }

        /// <summary>
        /// Gets the <see cref="ConstructorInfo"/> matching the given delegate type.
        /// </summary>
        /// <param name="delegateType">The delegate type.</param>
        /// <returns>The matching <see cref="ConstructorInfo"/>.</returns>
        public static ConstructorInfo GetConstructorInfo(Type delegateType)
        {
            if (!delegateType.IsSubclassOf(BaseDelegateType))
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(delegateType));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            var invokeReturnType = invokeMethodInfo.ReturnType;
            if (invokeReturnType == VoidType)
            {
                throw new InvalidOperationException("The return type of the delegate is incompatible (cannot be void).");
            }

            var invokeParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();

            var ctorInfo = invokeReturnType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                invokeParamTypes,
                new ParameterModifier[0]);

            if (ctorInfo == null)
            {
                throw new ArgumentException(
                    $"Cannot find constructor on type '{invokeReturnType.FullName}' matching delegate signature ({string.Join(", ", invokeParamTypes.Select(t => t.Name))}).",
                    nameof(delegateType));
            }

            return ctorInfo;
        }

        /// <summary>
        /// Gets a fast dynamic constructor delegate taking constructor parameter types.
        /// </summary>
        /// <param name="type">The type to construct.</param>
        /// <param name="ctorParamTypes">The parameter types of the target constructor.</param>
        /// <returns>A fast dynamic constructor delegate.</returns>
        public static FastDynamicDelegate GetConstructorEx(Type type, params Type[] ctorParamTypes)
        {
            var ctorInfo = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                ctorParamTypes,
                new ParameterModifier[0]);

            return GetConstructorEx(ctorInfo);
        }

        /// <summary>
        /// Gets a constructor delegate matching the given parameter types.
        /// </summary>
        /// <param name="type">The type to construct.</param>
        /// <param name="ctorParamTypes">The parameter types of the target constructor.</param>
        /// <returns>A compiled constructor delegate.</returns>
        public static Delegate GetConstructor(Type type, params Type[] ctorParamTypes)
        {
            var ctorInfo = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                ctorParamTypes,
                new ParameterModifier[0]);

            return GetConstructor(ctorInfo, GetFuncType([.. ctorParamTypes, type]));
        }

        /// <summary>
        /// Gets the default constructor delegate for the given type cast to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <returns>A parameterless constructor delegate.</returns>
        public static Func<T> GetDefaultConstructor<T>()
        {
            return CreateConstructor<Func<T>>();
        }

        /// <summary>
        /// Gets the default parameterless constructor delegate for the given type.
        /// </summary>
        /// <param name="type">The type to construct.</param>
        /// <returns>A parameterless constructor delegate.</returns>
        public static Func<object> GetDefaultConstructor(Type type)
        {
            var delegateType = GetFuncType(type);
            var ctorInfo = GetConstructorInfo(delegateType);
            return (Func<object>)GetConstructor(ctorInfo, GetFuncType(ObjectType));
        }

        /// <summary>
        /// Creates a delegate (of type <typeparamref name="T"/>) for the given constructor.
        /// </summary>
        /// <typeparam name="T">The delegate type.</typeparam>
        /// <param name="ctor">The constructor info.</param>
        /// <returns>A compiled constructor delegate.</returns>
        public static T CreateInstance<T>(ConstructorInfo ctor)
        {
            object result = GetConstructor(ctor, typeof(T));
            return (T)result;
        }

        /// <summary>
        /// Creates a delegate for the given constructor matching the specified delegate type.
        /// </summary>
        /// <param name="ctorInfo">The constructor info.</param>
        /// <param name="delegateType">The delegate type.</param>
        /// <returns>A compiled constructor delegate.</returns>
        public static Delegate GetConstructor(ConstructorInfo ctorInfo, Type delegateType)
        {
            Guard.Against.Null(ctorInfo, nameof(ctorInfo));
            Guard.Against.Null(delegateType, nameof(delegateType));
            var pair = (ctorInfo, delegateType);
            return TypedConstructorCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Creates a compiled constructor delegate matching <typeparamref name="TConstructorDelegate"/>.
        /// </summary>
        /// <typeparam name="TConstructorDelegate">The constructor delegate type.</typeparam>
        /// <param name="ctorInfo">The constructor info.</param>
        /// <returns>A compiled constructor delegate.</returns>
        public static TConstructorDelegate GetConstructor<TConstructorDelegate>(ConstructorInfo ctorInfo) where TConstructorDelegate : Delegate
        {
            var pair = (ctorInfo, typeof(TConstructorDelegate));
            var result = TypedConstructorCache.GetOrCreateValue(pair);
            return (TConstructorDelegate)result;
        }

        /// <summary>
        /// Gets a fast dynamic constructor delegate for the specified constructor info.
        /// </summary>
        /// <param name="ctor">The constructor info.</param>
        /// <returns>A fast dynamic constructor delegate.</returns>
        public static FastDynamicDelegate GetConstructorEx(ConstructorInfo ctor)
        {
            Guard.Against.Null(ctor, nameof(ctor));
            return ConstructorCache.GetOrCreateValue(ctor);
        }

        /// <summary>
        /// Builds a constructor delegate using DynamicMethod IL generation for maximum instantiation speed.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <returns>A compiled constructor delegate.</returns>
        public static TDelegate BuildConstructor<TDelegate>() where TDelegate : Delegate
        {
            return (TDelegate)BuildConstructor(typeof(TDelegate));
        }

        /// <summary>
        /// Builds a constructor delegate using DynamicMethod IL generation for maximum instantiation speed.
        /// </summary>
        /// <param name="delegateType">The delegate type.</param>
        /// <returns>A compiled constructor delegate.</returns>
        public static Delegate BuildConstructor(Type delegateType)
        {
            var ctorInfo = GetConstructorInfo(delegateType);
            var ctorParams = ctorInfo.GetParameters();
            var type = ctorInfo.DeclaringType;

            var ctorParamTypes = ctorParams.Length > 0 ? ctorParams.Select(p => p.ParameterType).ToArray() : Type.EmptyTypes;
            var dynMethod = new DynamicMethod(
                Guid.NewGuid().ToString("N"),
                type,
                ctorParamTypes,
                type,
                true);

            var ilGen = dynMethod.GetILGenerator();

            for (int i = 0; i < ctorParams.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        ilGen.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        ilGen.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        ilGen.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        ilGen.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        ilGen.Emit(OpCodes.Ldarg, i);
                        break;
                }
            }
            ilGen.Emit(OpCodes.Newobj, ctorInfo);
            ilGen.Emit(OpCodes.Ret);

            return dynMethod.CreateDelegate(delegateType);
        }

        private static Delegate GetConstructorInternal((ConstructorInfo, Type) pair)
        {
            Delegate result = default;
            BuildConstructorLambda(pair, (bodyExpr, paramExprs) =>
            {
                var lambdaExpr = Expression.Lambda(pair.Item2, bodyExpr, paramExprs);
                result = lambdaExpr.Compile();
            });
            return result;
        }

        private static void BuildConstructorLambda((ConstructorInfo, Type) pair, Action<Expression, ParameterExpression[]> builder)
        {
            var ctorInfo = pair.Item1;
            var delegateType = pair.Item2;

            if (!delegateType.IsSubclassOf(BaseDelegateType))
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(pair));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            if (invokeMethodInfo == null)
            {
                throw new InvalidOperationException("The given delegate type does not have an Invoke method.");
            }

            var type = ctorInfo.DeclaringType;
            var invokeReturnType = invokeMethodInfo.ReturnType;

            if (invokeReturnType == VoidType)
            {
                throw new InvalidOperationException("The return type of the delegate is incompatible.");
            }

            var invokeParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var ctorParams = ctorInfo.GetParameters();
            var ctorParamTypes = ctorParams.Length > 0 ? ctorParams.Select(pi => pi.ParameterType).ToArray() : Type.EmptyTypes;

            int count = invokeParamTypes.Length;
            if (ctorParamTypes.Length != count)
            {
                throw new InvalidOperationException("The number of parameters between the constructor and the delegate is not compatible.");
            }

            var paramExprs = new ParameterExpression[count];
            var argExprs = new Expression[count];
            for (var i = 0; i < count; i++)
            {
                var argument = GetArgumentExpression(i, ctorParamTypes, invokeParamTypes, paramExprs);
                argExprs[i] = argument;
            }

            Expression resultExpr = Expression.New(ctorInfo, argExprs);

            if (invokeReturnType != type)
            {
                resultExpr = Expression.Convert(resultExpr, invokeReturnType);
            }

            builder?.Invoke(resultExpr, paramExprs);
        }

        private static FastDynamicDelegate CreateConstructorEx(ConstructorInfo ctor)
        {
            var paramExpr = Expression.Parameter(typeof(object[]), "parameters");
            var varExprs = new List<ParameterExpression>();
            var beforeInstrExprs = new List<Expression>();
            var afterInstrExprs = new List<Expression>();

            Expression[] accessorExprs = null;
            var @params = ctor.GetParameters();
            int count = @params.Length;
            if (count != 0)
            {
                accessorExprs = new Expression[count];
                for (int i = 0; i < count; i++)
                {
                    var param = @params[i];
                    var paramType = param.ParameterType;

                    var constExpr = Expression.Constant(i);
                    Expression accessParamExpr = Expression.ArrayAccess(paramExpr, constExpr);

                    if (paramType.IsByRef)
                    {
                        paramType = paramType.GetElementType();

                        if (paramType != ObjectType)
                        {
                            var varExp = Expression.Variable(paramType);
                            varExprs.Add(varExp);
                            accessorExprs[i] = varExp;

                            if (!param.IsOut)
                            {
                                var effectiveAccessParamExpr = accessParamExpr;
                                if (paramType != ObjectType)
                                {
                                    effectiveAccessParamExpr = Expression.Convert(accessParamExpr, paramType);
                                }
                                var setInExpr = Expression.Assign(varExp, effectiveAccessParamExpr);
                                beforeInstrExprs.Add(setInExpr);
                            }

                            Expression accessVarExpr = varExp;
                            if (paramType != ObjectType)
                            {
                                accessVarExpr = Expression.Convert(varExp, ObjectType);
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

            var newExpr = Expression.New(ctor, accessorExprs);
            var instrExprs = new List<Expression>();
            instrExprs.AddRange(beforeInstrExprs);

            Expression bodyExpr = newExpr;
            if (ctor.DeclaringType != ObjectType)
            {
                bodyExpr = Expression.Convert(newExpr, ObjectType);
            }

            var resultVarExpr = Expression.Variable(ObjectType);
            varExprs.Add(resultVarExpr);

            bodyExpr = Expression.Assign(resultVarExpr, bodyExpr);
            instrExprs.Add(bodyExpr);
            instrExprs.AddRange(afterInstrExprs);
            instrExprs.Add(resultVarExpr);

            bodyExpr = Expression.Block(ObjectType, varExprs, instrExprs);

            var result = Expression.Lambda<FastDynamicDelegate>(bodyExpr, paramExpr);
            return result.Compile();
        }
    }
}
