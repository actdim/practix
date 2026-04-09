using ActDim.Practix.Collections.Concurrent;
using ActDim.Practix.TypeAccess.Linq.Dynamic;
using Ardalis.GuardClauses;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Security;
using System.Text;
using System.Xml.Linq;

// [assembly: AllowPartiallyTrustedCallers]
// [assembly: SecurityTransparent]
// [assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
namespace ActDim.Practix.TypeAccess.Reflection
{
    /// <summary>
    /// Various reflection related methods that are missing from the standard library.
    /// This class allows you to get members from types more safely than using
    /// string literals.
    /// </summary>
    public static class TypeAccessor
    {
        public static Type GetFuncType(params Type[] typeArgs)
        {
            // TODO: cache (memoize)
            // var result = Type.GetType($"System.Func`{typeArgs.Length}");
            // result = result.MakeGenericType(typeArgs);
            return Expression.GetFuncType(typeArgs);
        }

        public static Type GetActionType(params Type[] typeArgs)
        {
            // TODO: cache (memoize)
            return Expression.GetActionType(typeArgs);
        }

        #region GetMember
        /// <summary>
        /// Gets a member by it's expression usage.
        /// For example, GetMember(() => obj.GetType()) will return the
        /// GetType method.
        /// </summary>
        public static MemberInfo GetMemberInfo(LambdaExpression expr)
        {
            Guard.Against.Null(expr, nameof(expr));

            var bodyExpr = expr.Body;

            switch (bodyExpr.NodeType)
            {
                case ExpressionType.MemberAccess:
                    var memberExpr = (MemberExpression)bodyExpr;
                    return memberExpr.Member;

                case ExpressionType.Call:
                    var callExpr = (MethodCallExpression)bodyExpr;
                    return callExpr.Method;

                case ExpressionType.New:
                    var newExpr = (NewExpression)bodyExpr;
                    return newExpr.Constructor;
            }

            throw new ArgumentException($"{nameof(expr)}.Body must be a member or call expression.", nameof(expr));
        }
        #endregion
        #region GetConstructor
        /// <summary>
        /// Gets the constructor info from a sample construction call expression.
        /// Example: GetConstructor(() => new Control()) will return the constructor
        /// info for the default constructor of Control.
        /// </summary>
        public static ConstructorInfo GetConstructorInfo<T>(Expression<Func<T>> expr)
        {
            return (ConstructorInfo)GetMemberInfo(expr);
        }
        #endregion
        #region GetFieldInfo
        /// <summary>
        /// Gets a field from a sample usage.
        /// Example: GetField(() => Type.EmptyTypes) will return the FieldInfo of
        /// EmptyTypes.
        /// </summary>
        public static FieldInfo GetFieldInfo<T>(Expression<Func<T>> expr)
        {
            return (FieldInfo)GetMemberInfo(expr);
        }
        #endregion
        #region GetProperty

        /// <summary>
        /// Gets a property from a sample usage.
        /// Example: GetProperty(() => str.Length) will return the property info
        /// of Length.
        /// </summary>
        public static PropertyInfo GetPropertyInfo<T>(Expression<Func<T>> expr)
        {
            return (PropertyInfo)GetMemberInfo(expr);
        }

        public static PropertyInfo GetPropertyInfo<TInstance, T>(Expression<Func<TInstance, T>> expr)
        {
            return (PropertyInfo)GetMemberInfo(expr);
        }

        #endregion
        #region GetMethodInfo
        /// <summary>
        /// Gets a method info of a void method.
        /// Example: GetMethod(() => Console.WriteLine("")); will return the
        /// MethodInfo of WriteLine that receives a single argument.
        /// </summary>
        public static MethodInfo GetMethodInfo(Expression<Action> expr)
        {
            Guard.Against.Null(expr, nameof(expr));

            var bodyExpr = expr.Body;
            if (bodyExpr.NodeType != ExpressionType.Call)
            {
                throw new ArgumentException($"{nameof(expr)}.Body must be a Call expression.", nameof(expr));
            }

            MethodCallExpression callExpr = (MethodCallExpression)bodyExpr;
            return callExpr.Method;
        }

        /// <summary>
        /// Gets the MethodInfo of a method that returns a value.
        /// Example: GetMethod(() => Console.ReadLine()); will return the method info
        /// of ReadLine.
        /// </summary>
        public static MethodInfo GetMethodInfo<T>(Expression<Func<T>> expr)
        {
            return (MethodInfo)GetMemberInfo(expr);
        }
        #endregion

        #region TryGetDefaultConstructorDelegate
        /// <summary>
        /// Tries to get a default constructor
        /// null is returned if type does not have a public default constructor.
        /// </summary>
        public static Func<T> TryGetDefaultConstructorDelegate<T>()
        {
            var type = typeof(T);

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                return null;
            }

            return GetDefaultConstructor<T>();
        }
        #endregion
        #region GetDefaultConstructor

        private static Delegate CreateConstructorInternal(Type delegateType) // CreateCtor
        {
            var ctorInfo = GetConstructorInfo(delegateType);
            return GetConstructor(ctorInfo, delegateType);
        }

        public static FastDynamicDelegate CreateConstructorEx(Type delegateType) // CreateCtorEx
        {
            var ctorInfo = GetConstructorInfo(delegateType);
            return GetConstructorEx(ctorInfo);
        }

        public static TConstructorDelegate CreateConstructor<TConstructorDelegate>() where TConstructorDelegate : Delegate // CreateCtor
        {
            var ctorDelegateType = typeof(TConstructorDelegate);
            // return (TConstructorDelegate)CreateConstructorInternal(ctorDelegateType);
            var ctorInfo = GetConstructorInfo(ctorDelegateType);
            return GetConstructor<TConstructorDelegate>(ctorInfo);
        }

        public static ConstructorInfo GetConstructorInfo<TDelegate>()
        {
            return GetConstructorInfo(typeof(TDelegate));
        }

        // FindConstructorInfo
        public static ConstructorInfo GetConstructorInfo(Type delegateType)
        {
            if (!delegateType.IsSubclassOf(BaseDelegateType)) // !BaseDelegateType.IsAssignableFrom(delegateType)
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type");
            }

            // var methodInfos = delegateType.GetMethods(BindingFlags.Public);
            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            var invokeReturnType = invokeMethodInfo.ReturnType; // resultType
            if (invokeReturnType == VoidType)
            {
                // void type is not constructable
                throw new InvalidOperationException("The return type of the delegate is incompatible."); // cannot be void
            }

            var invokeParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();

            var ctorInfo = invokeReturnType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public, // BindingFlags.NonPublic?
                null,
                CallingConventions.HasThis,
                invokeParamTypes,
                new ParameterModifier[0]); // null

            if (ctorInfo == null)
            {
                // TODO: include signature to message
                throw new ArgumentException("Can't find constructor with delegate's signature", nameof(delegateType));
            }

            return ctorInfo;
        }

        public static FastDynamicDelegate GetConstructorEx(Type type, params Type[] ctorParamTypes) // GetCtorEx
        {
            var ctorInfo = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                ctorParamTypes,
                new ParameterModifier[0]); // null

            return GetConstructorEx(ctorInfo);
        }

        public static Delegate GetConstructor(Type type, params Type[] ctorParamTypes) // GetCtor
        {
            var ctorInfo = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                ctorParamTypes,
                new ParameterModifier[0]); // null

            return GetConstructor(ctorInfo, GetFuncType([.. ctorParamTypes, type]));
        }

        /// <summary>
        /// Gets the default constructor for the given objectType, but return
        /// it already casted to a given "T".
        /// </summary>
        public static Func<T> GetDefaultConstructor<T>()
        {
            // return BuildConstructor<Func<T>>();
            var result = CreateConstructor<Func<T>>();
            return result;
        }

        public static Func<object> GetDefaultConstructor(Type type)
        {
            var delegateType = GetFuncType(type);
            // var ctor = CreateConstructorEx(delegateType);
            // var ctor = GetConstructorEx(type, Type.EmptyTypes);
            // var result = () => ctor();
            var ctorInfo = GetConstructorInfo(delegateType);
            var result = (Func<object>)GetConstructor(ctorInfo, GetFuncType(ObjectType));
            return result;
        }

        #endregion
        #region GetConstructor<T>
        /// <summary>
        /// Creates a delegate (of type T) for the given constructor.
        /// The delegate type should match the number of parameters in the constructor.
        /// Casts are done if required but no other conversions are done.
        /// </summary>
        public static T CreateInstance<T>(ConstructorInfo ctor)
        {
            object result = GetConstructor(ctor, typeof(T));
            return (T)result;
        }

        // public static object CreateInstance(ConstructorInfo ctor)
        // {
        // 	object result = GetConstructor(ctor);
        // 	return (T)result;
        // }

        private static readonly Func<(ConstructorInfo, Type), Delegate> GetTypedConstructorDelegate = GetConstructorInternal;
        private static readonly ConcurrentFactoryDictionary<(ConstructorInfo, Type), Delegate> TypedConstructorCache = new ConcurrentFactoryDictionary<(ConstructorInfo, Type), Delegate>(GetTypedConstructorDelegate);

        /// <summary>
        /// Creates a delegate for the given constructor.
        /// The delegate type should match the number of parameters in the constructor.
        /// Casts are done if required but no other conversions are done.
        /// </summary>
        public static Delegate GetConstructor(ConstructorInfo ctorInfo, Type delegateType) // GetCtor
        {
            Guard.Against.Null(ctorInfo, nameof(ctorInfo));
            Guard.Against.Null(delegateType, nameof(delegateType));
            var pair = (ctorInfo, delegateType);
            var result = TypedConstructorCache.GetOrCreateValue(pair);
            return result;
        }

        public static TDelegate BuildConstructor<TDelegate>() where TDelegate : Delegate
        {
            return (TDelegate)BuildConstructor(typeof(TDelegate));
        }

        public static Delegate BuildConstructor(Type delegateType)
        {
            // https://github.com/FluentIL/FluentIL
            // https://github.aiurs.co/Nyrest/FastGenericNew/

            var ctorInfo = GetConstructorInfo(delegateType);

            var ctorParams = ctorInfo.GetParameters();

            var type = ctorInfo.DeclaringType;

            // bool isVisible = ctorInfo.DeclaringType.IsVisible && (ctorInfo.IsPublic && !ctorInfo.IsFamilyOrAssembly);

            var ctorParamTypes = ctorParams.Length > 0 ? ctorParams.Select(p => p.ParameterType).ToArray() : Type.EmptyTypes;
            // type.FullName + ".ctor"
            var dynMethod = new DynamicMethod(Guid.NewGuid().ToString("N"),
                type,
                ctorParamTypes,
                type,
                // ctorInfo.Module, // typeof(TypeAccessor).Module
                true); // !isVisible

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
                // ilGen.Emit(OpCodes.Ldarg, i);  // Load argument onto the stack
                // ilGen.Emit(ctorParamTypes[i].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, ctorParamTypes[i]); // Cast or unbox
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

        private static TConstructorDelegate GetConstructorInternal<TConstructorDelegate>((ConstructorInfo, Type) pair)
        {
            TConstructorDelegate result = default;

            BuildConstructorLambda(pair, (bodyExpr, paramExprs) =>
            {
                var lambdaExpr = Expression.Lambda<TConstructorDelegate>(bodyExpr, paramExprs);

                result = lambdaExpr.Compile();
            });

            return result;
        }

        public static TConstructorDelegate GetConstructor<TConstructorDelegate>(ConstructorInfo ctorInfo) where TConstructorDelegate : Delegate // GetCtor
        {
            var pair = (ctorInfo, typeof(TConstructorDelegate));
            var result = TypedConstructorCache.GetOrCreateValue(pair);
            return (TConstructorDelegate)result;
        }

        private static void BuildConstructorLambda((ConstructorInfo, Type) pair, Action<Expression, ParameterExpression[]> builder)
        {
            var ctorInfo = pair.Item1;
            var delegateType = pair.Item2;

            if (!delegateType.IsSubclassOf(BaseDelegateType)) // !BaseDelegateType.IsAssignableFrom(delegateType)
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(pair));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            if (invokeMethodInfo == null)
            {
                throw new InvalidOperationException("The given delegate type does not have an Invoke method.");
            }

            var type = ctorInfo.DeclaringType;
            var invokeReturnType = invokeMethodInfo.ReturnType; // resultType

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

            if (invokeReturnType != type) // && (type.IsValueType || !invokeReturnType.IsAssignableFrom(type))
            {
                resultExpr = Expression.Convert(resultExpr, invokeReturnType);
            }

            if (builder != null)
            {
                builder(resultExpr, paramExprs);
            }
        }
        #endregion

        #region GetConstructor

        private static readonly Func<ConstructorInfo, FastDynamicDelegate> GetConstructorDelegate = CreateConstructorEx;
        private static readonly ConcurrentFactoryDictionary<ConstructorInfo, FastDynamicDelegate> ConstructorCache = new ConcurrentFactoryDictionary<ConstructorInfo, FastDynamicDelegate>(GetConstructorDelegate);

        /// <summary>
        /// Gets a constructor invoker delegate for the given constructor info.
        /// Using the delegate is much faster than calling the Invoke method on the constructor,
        /// but if you invoke it only once, it will do no good as some time is spent compiling
        /// such delegate.
        /// </summary>
        public static FastDynamicDelegate GetConstructorEx(ConstructorInfo ctor)
        {
            Guard.Against.Null(ctor, nameof(ctor));

            var result = ConstructorCache.GetOrCreateValue(ctor);
            return result;
        }

        private static FastDynamicDelegate CreateConstructorEx(ConstructorInfo ctor) // CreateCtorEx
        {
            // TODO: check https://exchangetuts.com/c-emitting-dynamic-method-delegate-to-load-parametrized-constructor-problem-1641701285304750

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
                    // Expression accessParamExpr = Expression.ArrayIndex(paramExpression, constExpr);

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
                                // var setInExpr = ExpressionParser.GenerateAssign(varExp, effectiveAccessParamExpr);

                                beforeInstrExprs.Add(setInExpr);
                            }

                            Expression accessVarExpr = varExp;
                            if (paramType != ObjectType)
                            {
                                accessVarExpr = Expression.Convert(varExp, ObjectType);
                            }
                            var setOutExpr = Expression.Assign(accessParamExpr, accessVarExpr);
                            // var setOutExpr = ExpressionParser.GenerateAssign(accessParamExpr, accessVarExpr);

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
            // var returnTargetExpr = Expression.Label(ObjectType);
            var instrExprs = new List<Expression>();
            instrExprs.AddRange(beforeInstrExprs);

            ParameterExpression resultVarExpr = null;
            Expression bodyExpr = newExpr;
            if (ctor.DeclaringType != ObjectType)
            {
                bodyExpr = Expression.Convert(newExpr, ObjectType);
            }

            resultVarExpr = Expression.Variable(ObjectType);
            varExprs.Add(resultVarExpr);

            bodyExpr = Expression.Assign(resultVarExpr, bodyExpr);
            // bodyExpr = ExpressionParser.GenerateAssign(resultVarExpr, bodyExpr);

            instrExprs.Add(bodyExpr);

            instrExprs.AddRange(afterInstrExprs);
            // var returnExpr = Expression.Return(returnTargetExpr, resultVarExpr);
            // instrExprs.Add(returnExpr);
            instrExprs.Add(resultVarExpr);

            // instrExprs.Add(Expression.Label(returnTargetExpr, Expression.Constant(null, ObjectType)));
            bodyExpr = Expression.Block(ObjectType, varExprs, instrExprs);

            var result = Expression.Lambda<FastDynamicDelegate>(bodyExpr, paramExpr);

            return result.Compile();
        }

        #endregion
        #region GetMethodCaller
        private static readonly ConcurrentFactoryDictionary<MethodInfo, FastMethodCallDelegate> MethodCallerCache = new ConcurrentFactoryDictionary<MethodInfo, FastMethodCallDelegate>(CreateMethodCaller);
        /// <summary>
        /// Gets a delegate to call the given method in a fast manner.
        /// </summary>
        public static FastMethodCallDelegate GetMethodCaller(MethodInfo method)
        {
            var result = MethodCallerCache.GetOrCreateValue(method);
            return result;
        }
        private static FastMethodCallDelegate CreateMethodCaller(MethodInfo methodInfo)
        {
            var paramExpr = Expression.Parameter(typeof(object[]));

            ParameterExpression targetExpr = Expression.Parameter(ObjectType);

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
                    // Expression accessParamExpr = Expression.ArrayIndex(paramExpr, constExpr);

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
                                // var setInExpr = ExpressionParser.GenerateAssign(varExpr, effectiveAccessParamExpr);

                                beforeInstrExprs.Add(setInExpr);
                            }

                            Expression accessVarExpr = varExpr;
                            if (paramType != ObjectType)
                            {
                                accessVarExpr = Expression.Convert(varExpr, ObjectType);
                            }

                            var setOutExpr = Expression.Assign(accessParamExpr, accessVarExpr);
                            // var setOutExpr = ExpressionParser.GenerateAssign(accessParamExpr, accessVarExpr);

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

            // var returnTargetExpr = Expression.Label(ObjectType);
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
                // bodyExpr = ExpressionParser.GenerateAssign(resultVarExpr, bodyExpr);

            }
            instrExprs.Add(bodyExpr);

            instrExprs.AddRange(afterInstrExprs);

            if (methodInfo.ReturnType == VoidType)
            {
                // var returnExpr = Expression.Return(returnTargetExpr, Expression.Constant(null, ObjectType), ObjectType);
                // instrExprs.Add(returnExpr);
                instrExprs.Add(Expression.Constant(null, ObjectType));
            }
            else
            {
                // var returnExpr = Expression.Return(returnTargetExpr, resultVarExpr);
                // instrExprs.Add(returnExpr);
                instrExprs.Add(resultVarExpr);
            }

            // instrExprs.Add(Expression.Label(returnTargetExpr, Expression.Constant(null, ObjectType)));
            bodyExpr = Expression.Block(ObjectType, varExprs, instrExprs);

            var result = Expression.Lambda<FastMethodCallDelegate>(bodyExpr, targetExpr, paramExpr);

            return result.Compile();
        }
        #endregion


        #region GetMethodCaller<T>
        /// <summary>
        /// Creates a method call delegate for the given method info.
        /// The delegateType (T) should have the same amount of parameters as the method. Note
        /// that non-static methods have a first parameter to represent the instance.
        /// </summary>
        public static T GetMethodCaller<T>(MethodInfo method)
        {
            object result = GetMethodCaller(method, typeof(T));
            return (T)result;
        }

        private static readonly Func<(MethodInfo, Type), Delegate> GetTypedMethodCallerDelegate = GetTypedMethodCaller; // GetTypedMethodInvoker
        private static readonly ConcurrentFactoryDictionary<(MethodInfo, Type), Delegate> TypedMethodCallerCache = new ConcurrentFactoryDictionary<(MethodInfo, Type), Delegate>(GetTypedMethodCallerDelegate); //TypedMethodInvokersCache

        /// <summary>
        /// Creates a method call delegate for the given method info.
        /// The delegateType should have the same amount of parameters as the method. Note
        /// that non-static methods have a first parameter to represent the instance.
        /// </summary>
        public static Delegate GetMethodCaller(MethodInfo method, Type delegateType)
        {
            Guard.Against.Null(method, nameof(method));

            if (!delegateType.IsSubclassOf(typeof(Delegate)))
            {
                throw new ArgumentException($"{nameof(delegateType)} is not a Delegate.", nameof(delegateType));
            }

            var pair = (method, delegateType);
            var result = TypedMethodCallerCache.GetOrCreateValue(pair);
            return result;
        }
        private static Delegate GetTypedMethodCaller((MethodInfo, Type) pair)
        {
            var method = pair.Item1;
            var delegateType = pair.Item2;

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            if (invokeMethodInfo == null)
            {
                throw new InvalidOperationException("The given delegate type does not have an Invoke method. Is this a compilation error?");
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

            var result = lambdaExpr.Compile();

            return result;
        }
        private static Expression GetArgumentExpression(int index, IList<Type> methodParameterTypes, Type[] invokeParameterTypes, ParameterExpression[] paramExprs)
        {
            var invokeParameterType = invokeParameterTypes[index];
            var methodParameterType = methodParameterTypes[index];

            var paramExpr = Expression.Parameter(invokeParameterType, "P" + index);
            paramExprs[index] = paramExpr;
            if (methodParameterType == invokeParameterType)
            {
                return paramExpr;
            }

            var convertExpr = Expression.Convert(paramExpr, methodParameterType);
            return convertExpr;
        }
        #endregion

        #region GetPropertyGetter


        private static readonly Func<(Type, PropertyInfo), Delegate> GetTypedPropertyGetterDelegate = GetPropertyGetter;
        private static readonly ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate> TypedPropertyGetterCache = new ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate>(GetTypedPropertyGetterDelegate);

        /// <summary>
        /// Gets a delegate to read values from the given property in a very fast manner.
        /// The result will be already cast or will even avoid casts if the
        /// generic types are correct.
        /// </summary>
        public static Func<TInstance, TOutput> GetPropertyGetter<TInstance, TOutput>(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));

            var pair = (typeof(Func<TInstance, TOutput>), propInfo);
            var result = TypedPropertyGetterCache.GetOrCreateValue(pair);
            return (Func<TInstance, TOutput>)result;
        }

        /// <summary>
        /// Gets a delegate to read values from the given property in a very fast manner.
        /// </summary>
        public static Delegate GetPropertyGetter(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));

            var pair = (typeof(Delegate), propInfo);
            var result = TypedPropertyGetterCache.GetOrCreateValue(pair);
            return result;
        }

        public static Delegate GetPropertyGetter(Type type, string name)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.NullOrEmpty(name, nameof(name));

            var propInfo = type.GetProperty(name);
            var result = GetPropertyGetter(propInfo);
            return result;
        }

        public static Func<T, TProperty> GetPropertyGetter<T, TProperty>(string name)
        {
            return (Func<T, TProperty>)GetPropertyGetter(typeof(T), name);
        }

        public static Delegate GetFieldGetter(Type type, string name)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.NullOrEmpty(name, nameof(name));

            var fieldInfo = type.GetField(name);
            var result = GetFieldGetter(fieldInfo);
            return result;
        }

        public static Func<T, TField> GetFieldGetter<T, TField>(string name)
        {
            return (Func<T, TField>)GetFieldGetter(typeof(T), name);
        }

        public static TDelegate GetStaticMethodCaller<T, TDelegate>(string name)
        {
            return GetStaticMethodCaller<TDelegate>(typeof(T), name);
        }

        private static readonly Type BaseDelegateType = typeof(Delegate);
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type VoidType = typeof(void);

        public static TDelegate GetStaticMethodCaller<TDelegate>(Type type, string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var delegateType = typeof(TDelegate);
            if (!delegateType.IsSubclassOf(BaseDelegateType)) // !BaseDelegateType.IsAssignableFrom(delegateType)
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(TDelegate));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            var invokeParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                invokeParamTypes,
                new ParameterModifier[0]); // null

            var caller = GetMethodCaller<TDelegate>(methodInfo);
            return caller;
        }

        public static TDelegate GetMethodCaller<T, TDelegate>(string name)
        {
            return GetMethodCaller<TDelegate>(typeof(T), name);
        }

        public static TDelegate GetMethodCaller<TDelegate>(Type type, string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var delegateType = typeof(TDelegate);
            if (!delegateType.IsSubclassOf(BaseDelegateType)) // !BaseDelegateType.IsAssignableFrom(delegateType)
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type", nameof(TDelegate));
            }

            var invokeMethodInfo = delegateType.GetMethod("Invoke");
            var delegateParamTypes = invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToList();
            if (delegateParamTypes[0] != type)
            {
                // we are getting non-static method caller
                throw new ArgumentException($"Invalid delegate type ({delegateType.FullName}). First parameter should be of type {type.FullName} (to represent the instance).", nameof(TDelegate));
            }
            delegateParamTypes.RemoveAt(0);

            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [.. delegateParamTypes],
                new ParameterModifier[0]); // null

            var caller = GetMethodCaller<TDelegate>(methodInfo);
            return caller;
        }

        private static readonly Type DelegateType = typeof(Delegate);

        private static Delegate GetPropertyGetter((Type, PropertyInfo) pair)
        {
            var delegateType = pair.Item1;
            var propInfo = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments(); // delegateTypeArgs
            var instanceType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[0] : propInfo.ReflectedType ?? propInfo.DeclaringType;
            var resultType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[1] : propInfo.PropertyType;

            var paramExpr = Expression.Parameter(instanceType, "instance");
            Expression resultExpr;

            var getMethod = propInfo.GetGetMethod();
            if (getMethod == null)
            {
                throw new ArgumentException($"Property {propInfo.Name} can't be read.", nameof(pair));
            }

            if (getMethod.IsStatic)
            {
                resultExpr = Expression.MakeMemberAccess(null, propInfo);
            }
            else
            {
                Expression readParamExpr = paramExpr;

                if (propInfo.DeclaringType != instanceType)
                {
                    readParamExpr = Expression.Convert(paramExpr, propInfo.DeclaringType);
                }

                resultExpr = Expression.MakeMemberAccess(readParamExpr, propInfo);
            }

            if (propInfo.PropertyType != resultType)
            {
                resultExpr = Expression.Convert(resultExpr, resultType);
            }

            LambdaExpression lambdaExpr;

            if (delegateType == DelegateType)
            {
                lambdaExpr = Expression.Lambda(resultExpr, paramExpr);
            }
            else
            {
                lambdaExpr = Expression.Lambda(delegateType, resultExpr, paramExpr);
            }

            var result = lambdaExpr.Compile();

            return result;
        }
        #endregion
        #region GetPropertySetter

        /// <summary>
        /// Gets a delegate that can be used to do very fast sets on the given property.
        /// </summary>
        public static Delegate GetPropertySetter(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));

            var pair = (typeof(Delegate), propInfo);
            var result = TypedPropertySetterCache.GetOrCreateValue(pair);
            return result;
        }

        private static readonly Func<(Type, PropertyInfo), Delegate> GetTypedPropertySetterDelegate = GetPropertySetter;
        private static readonly ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate> TypedPropertySetterCache = new ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate>(GetTypedPropertySetterDelegate);

        /// <summary>
        /// Gets a delegate that can be used to do very fast sets on the given property.
        /// If the generic types are correct, casts can be avoided to improve performance
        /// even further.
        /// </summary>
        public static Action<TInstance, TValue> GetPropertySetter<TInstance, TValue>(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));

            var pair = (typeof(Action<TInstance, TValue>), propInfo);
            var result = TypedPropertySetterCache.GetOrCreateValue(pair);
            return (Action<TInstance, TValue>)result;
        }

        private static Delegate GetPropertySetter((Type, PropertyInfo) pair)
        {
            var delegateType = pair.Item1;
            var propInfo = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments(); // delegateTypeArgs
            var instanceType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[0] : propInfo.ReflectedType ?? propInfo.DeclaringType;
            var valueType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[1] : propInfo.PropertyType;

            var instanceParamExpr = Expression.Parameter(instanceType, "instance"); //this

            var valueParamExpr = Expression.Parameter(valueType, "value");

            Expression readValueParamExpr = valueParamExpr;
            if (propInfo.PropertyType != valueType)
            {
                readValueParamExpr = Expression.Convert(valueParamExpr, propInfo.PropertyType);
            }

            // .Net 3.5 does not have assign
            // but we can call the set method directly (and we need it to test for static).
            var setMethod = propInfo.GetSetMethod(true);
            if (setMethod == null)
            {
                throw new ArgumentException($"Property {propInfo.Name} is read-only.", nameof(pair));
            }

            Expression setExpr;
            if (setMethod.IsStatic)
            {
                setExpr = Expression.Call(setMethod, readValueParamExpr);
            }
            else
            {
                Expression readInstanceParamExpr = instanceParamExpr;
                if (propInfo.DeclaringType != instanceType)
                {
                    readInstanceParamExpr = Expression.Convert(instanceParamExpr, propInfo.DeclaringType);
                }
                setExpr = Expression.Call(readInstanceParamExpr, setMethod, readValueParamExpr);
                // var accessFieldExpr = Expression.Property(readInstanceParamExpr, propInfo);
                // var accessFieldExpr = Expression.PropertyOrField(readInstanceParamExpr, propInfo.Name);
                // var accessFieldExpr = Expression.MakeMemberAccess(readInstanceParamExpr, propInfo);
                // setExpr = Expression.Assign(accessFieldExpr, readValueParamExpr);
                // // setExpr = ExpressionParser.GenerateAssign(accessFieldExpr, readValueParamExpr);
            }

            LambdaExpression lambdaExpr;

            if (delegateType == DelegateType)
            {
                lambdaExpr = Expression.Lambda(setExpr, instanceParamExpr, valueParamExpr);
            }
            else
            {
                lambdaExpr = Expression.Lambda(delegateType, setExpr, instanceParamExpr, valueParamExpr);
            }

            var result = lambdaExpr.Compile();

            return result;
        }
        #endregion
        #region GetEventAdder

        private static readonly Func<EventInfo, Action<object, Delegate>> GetEventAdderDelegate = GetEventAdder<object, Delegate>;
        private static readonly ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>> EventAdderCache = new ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>>(GetEventAdderDelegate);

        /// <summary>
        /// Gets a delegate to do fast "event add"
        /// </summary>
        public static Action<object, Delegate> GetEventAdder(EventInfo eventInfo)
        {
            var result = EventAdderCache.GetOrCreateValue(eventInfo);
            return result;
        }

        /// <summary>
        /// Gets a delegate to do fast "event add".
        /// Can avoid casts if the right generic types are given.
        /// </summary>
        public static Action<TInstance, TDelegate> GetEventAdder<TInstance, TDelegate>(EventInfo eventInfo)
        {
            return GetEventDelegate<TInstance, TDelegate>(eventInfo.GetAddMethod(), eventInfo.EventHandlerType);
        }
        #endregion
        #region GetEventRemover

        private static readonly Func<EventInfo, Action<object, Delegate>> GetEventRemoverDelegate = GetEventRemover<object, Delegate>;
        private static readonly ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>> EventRemoverCache = new ConcurrentFactoryDictionary<EventInfo, Action<object, Delegate>>(GetEventRemoverDelegate);

        /// <summary>
        /// Gets a delegate to do fast "event remove" calls.
        /// </summary>
        public static Action<object, Delegate> GetEventRemover(EventInfo eventInfo)
        {
            var result = EventRemoverCache.GetOrCreateValue(eventInfo);
            return result;
        }
        /// <summary>
        /// Gets a delegate to do fast "event remove" calls.
        /// Can avoid casts if the right generic types are given.
        /// </summary>
        public static Action<TInstance, TDelegate> GetEventRemover<TInstance, TDelegate>(EventInfo eventInfo)
        {
            return GetEventDelegate<TInstance, TDelegate>(eventInfo.GetRemoveMethod(), eventInfo.EventHandlerType);
        }
        #endregion
        #region GetEventDelegate
        private static Action<TInstance, TDelegate> GetEventDelegate<TInstance, TDelegate>(MethodInfo methodInfo, Type handlerType)
        {
            var instanceParamExpr = Expression.Parameter(typeof(TInstance), "instance");
            var handlerParamExpr = Expression.Parameter(typeof(TDelegate), "handler");
            Expression readHandlerParamExpr = handlerParamExpr;
            if (handlerType != typeof(TDelegate))
            {
                readHandlerParamExpr = Expression.Convert(handlerParamExpr, handlerType);
            }
            Expression callExpr;

            if (methodInfo.IsStatic)
            {
                callExpr = Expression.Call(methodInfo, readHandlerParamExpr);
            }
            else
            {
                Expression readInstanceParamExpr = instanceParamExpr;
                if (methodInfo.DeclaringType != typeof(TInstance))
                {
                    readInstanceParamExpr = Expression.Convert(instanceParamExpr, methodInfo.DeclaringType);
                }
                callExpr = Expression.Call(readInstanceParamExpr, methodInfo, readHandlerParamExpr);
            }

            var lambdaExpr = Expression.Lambda<Action<TInstance, TDelegate>>(callExpr, instanceParamExpr, handlerParamExpr);

            var result = lambdaExpr.Compile();

            return result;
        }
        #endregion

        #region GetFieldGetter

        /// <summary>
        /// Gets a delegate to read values from the given field in a very fast manner.
        /// </summary>
        public static Delegate GetFieldGetter(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));
            var pair = (typeof(Delegate), fieldInfo);
            var result = TypedFieldGetterCache.GetOrCreateValue(pair);
            return result;
        }

        private static readonly Func<(Type, FieldInfo), Delegate> GetTypedFieldGetterDelegate = GetFieldGetter;
        private static readonly ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate> TypedFieldGetterCache = new ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate>(GetTypedFieldGetterDelegate);

        /// <summary>
        /// Gets a delegate to read values from the given field in a very fast manner.
        /// The result will be already cast or will even avoid casts if the
        /// generic types are correct.
        /// </summary>
        public static Func<T, TField> GetFieldGetter<T, TField>(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));
            var pair = (typeof(Func<T, TField>), fieldInfo);
            var result = TypedFieldGetterCache.GetOrCreateValue(pair);
            return (Func<T, TField>)result;
        }

        private static Delegate GetFieldGetter((Type, FieldInfo) pair)
        {
            var delegateType = pair.Item1;
            var field = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments(); // delegateTypeArgs
            var instanceType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[0] : field.ReflectedType ?? field.DeclaringType;
            var resultType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[1] : field.FieldType;

            var paramExpr = Expression.Parameter(instanceType, "instance");
            Expression resultExpr;

            if (field.IsStatic)
            {
                resultExpr = Expression.MakeMemberAccess(null, field); //Expression.Field?
            }
            else
            {
                Expression readParamExpr = paramExpr;

                if (field.DeclaringType != instanceType)
                {
                    readParamExpr = Expression.Convert(paramExpr, field.DeclaringType);
                }

                resultExpr = Expression.MakeMemberAccess(readParamExpr, field); //Expression.Field?
            }

            if (field.FieldType != resultType)
            {
                resultExpr = Expression.Convert(resultExpr, resultType);
            }

            LambdaExpression lambdaExpr;

            if (delegateType == DelegateType)
            {
                lambdaExpr = Expression.Lambda(resultExpr, paramExpr);
            }
            else
            {
                lambdaExpr = Expression.Lambda(delegateType, resultExpr, paramExpr);
            }

            var result = lambdaExpr.Compile();

            return result;
        }
        #endregion
        #region GetFieldSetter

        /// <summary>
        /// Gets a delegate to write values to the given field in a very fast manner.
        /// </summary>
        public static Delegate GetFieldSetter(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));

            var pair = (typeof(Delegate), fieldInfo);
            var result = TypedFieldSetterCache.GetOrCreateValue(pair);
            return result;
        }

        private static readonly Func<(Type, FieldInfo), Delegate> GetTypedFieldSetterDelegate = GetFieldSetter;
        private static readonly ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate> TypedFieldSetterCache = new ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate>(GetTypedFieldSetterDelegate);

        /// <summary>
        /// Gets a delegate to write values to the given field in a very fast manner.
        /// The result will be already cast or will even avoid casts if the
        /// generic types are correct.
        /// </summary>
        public static Action<T, TField> GetFieldSetter<T, TField>(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));

            var pair = (typeof(Action<T, TField>), fieldInfo);
            var result = TypedFieldSetterCache.GetOrCreateValue(pair);
            return (Action<T, TField>)result;
        }

        private static Delegate GetFieldSetter((Type, FieldInfo) pair)
        {
            var delegateType = pair.Item1;
            var fieldInfo = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments(); // delegateTypeArgs
            var instanceType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[0] : fieldInfo.ReflectedType ?? fieldInfo.DeclaringType;
            var valType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[1] : fieldInfo.FieldType;

            var instanceParamExpr = Expression.Parameter(instanceType, "instance");

            var valParamExpr = Expression.Parameter(valType, "value");
            Expression readValueParamExpr = valParamExpr;

            if (fieldInfo.FieldType != valType)
            {
                readValueParamExpr = Expression.Convert(valParamExpr, fieldInfo.FieldType);
            }

            Expression accessFieldExpr;

            if (fieldInfo.IsStatic)
            {
                accessFieldExpr = Expression.MakeMemberAccess(null, fieldInfo); // Expression.Field?
            }
            else
            {
                Expression readInstanceParamExpr = instanceParamExpr;
                if (fieldInfo.DeclaringType != instanceType)
                {
                    readInstanceParamExpr = Expression.Convert(instanceParamExpr, fieldInfo.DeclaringType);
                }

                accessFieldExpr = Expression.MakeMemberAccess(readInstanceParamExpr, fieldInfo); // Expression.Field?
            }

            var assignExpr = Expression.Assign(accessFieldExpr, readValueParamExpr);
            // var assignExpr = ExpressionParser.GenerateAssign(accessFieldExpr, readValueParamExpr);

            LambdaExpression lambdaExpr;

            if (delegateType == DelegateType)
            {
                lambdaExpr = Expression.Lambda(assignExpr, instanceParamExpr, valParamExpr);
            }
            else
            {
                lambdaExpr = Expression.Lambda(delegateType, assignExpr, instanceParamExpr, valParamExpr);
            }

            var result = lambdaExpr.Compile();

            return result;
        }

        #endregion

        #region GetFastDynamicDelegate
        /// <summary>
        /// You have an untyped delegate? Then get another delegate to invoke it faster.
        /// </summary>
        public static FastDynamicDelegate GetFastDynamicDelegate(Delegate realDelegate)
        {
            var result = GetMethodCaller(realDelegate.Method);
            return (parameters) => result(realDelegate.Target, parameters);
        }
        #endregion

    }

    /// <summary>
    /// This is a typed version of reflection helper, so your expression already starts with a know
    /// object type (used when you don't have an already instantiated object).
    /// </summary>
    public static class TypeAccessor<T>
    {
        #region GetMemberInfo
        /// <summary>
        /// Gets a member by it's expression usage.
        /// For example, GetMember((obj) => obj.GetType()) will return the
        /// GetType method.
        /// </summary>
        public static MemberInfo GetMemberInfo<TOutput>(Expression<Func<T, TOutput>> expr)
        {
            Guard.Against.Null(expr, nameof(expr));

            var bodyExpr = expr.Body;

            while (true)
            {
                switch (bodyExpr.NodeType)
                {
                    case ExpressionType.Convert:
                        {
                            UnaryExpression convertExpr = (UnaryExpression)bodyExpr;
                            bodyExpr = convertExpr.Operand;
                            continue;
                        }

                    case ExpressionType.MemberAccess:
                        MemberExpression memberExpr = (MemberExpression)bodyExpr;
                        return memberExpr.Member;

                    case ExpressionType.Call:
                        MethodCallExpression callExpr = (MethodCallExpression)bodyExpr;
                        return callExpr.Method;

                    case ExpressionType.New:
                        NewExpression newExpr = (NewExpression)bodyExpr;
                        return newExpr.Constructor;
                }

                throw new ArgumentException($"{nameof(expr)}.Body must be a member or call expression.", nameof(expr));
            }
        }
        #endregion
        #region GetFieldInfo
        /// <summary>
        /// Gets a field from a sample usage.
        /// Example: GetField((obj) => obj.SomeField) will return the FieldInfo of
        /// EmptyTypes.
        /// </summary>
        public static FieldInfo GetFieldInfo<TField>(Expression<Func<T, TField>> expr)
        {
            return (FieldInfo)GetMemberInfo(expr);
        }
        #endregion
        #region GetPropertyInfo
        /// <summary>
        /// Gets a property from a sample usage.
        /// Example: GetProperty((str) => str.Length) will return the property info
        /// of Length.
        /// </summary>
        public static PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<T, TProperty>> expr)
        {
            return (PropertyInfo)GetMemberInfo(expr);
        }
        #endregion
        #region GetMethodInfo
        /// <summary>
        /// Gets a method info of a void method.
        /// Example: GetMethod((obj) => obj.SomeCall("")); will return the
        /// MethodInfo of SomeCall that receives a single argument.
        /// </summary>
        public static MethodInfo GetMethodInfo(Expression<Action<T>> expr)
        {
            Guard.Against.Null(expr, nameof(expr));

            var bodyExpr = expr.Body;
            if (bodyExpr.NodeType != ExpressionType.Call)
            {
                throw new ArgumentException($"{nameof(expr)}.Body must be a Call expression.", nameof(expr));
            }

            var callExpr = (MethodCallExpression)bodyExpr;
            return callExpr.Method;
        }

        /// <summary>
        /// Gets the MethodInfo of a method that returns a value.
        /// Example: GetMethod((obj) => obj.SomeCall()); will return the method info
        /// of SomeCall.
        /// </summary>
        public static MethodInfo GetMethodInfo<TOutput>(Expression<Func<T, TOutput>> expr)
        {
            return (MethodInfo)GetMemberInfo(expr);
        }
        #endregion

        public static Func<T, TProperty> GetPropertyGetter<TProperty>(PropertyInfo propInfo)
        {
            return TypeAccessor.GetPropertyGetter<T, TProperty>(propInfo);
        }

        public static Func<T, TField> GetFieldGetter<TField>(FieldInfo fieldInfo)
        {
            return TypeAccessor.GetFieldGetter<T, TField>(fieldInfo);
        }

        public static Func<T, TProperty> GetPropertyGetter<TProperty>(string name)
        {
            return TypeAccessor.GetPropertyGetter<T, TProperty>(name);
        }

        public static TProperty GetProperty<TProperty>(T obj, string name)
        {
            return GetPropertyGetter<TProperty>(name)(obj);
        }

        public static Func<T, TField> GetFieldGetter<TField>(string name)
        {
            return TypeAccessor.GetFieldGetter<T, TField>(name);
        }

        public static TField GetField<TField>(T obj, string name)
        {
            return GetFieldGetter<TField>(name)(obj);
        }

        public static TDelegate GetStaticMethodCaller<TDelegate>(string name)
        {
            return TypeAccessor.GetStaticMethodCaller<T, TDelegate>(name);
        }

        public static TDelegate GetMethodCaller<TDelegate>(string name)
        {
            return TypeAccessor.GetMethodCaller<T, TDelegate>(name);
        }
    }
}
