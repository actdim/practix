using ActDim.Practix.TypeAccess.Reflection;
using System;
using System.Linq;
using System.Reflection;

namespace ActDim.Practix.TypeAccess.Linq
{
    public static class TypeExtensions
    {
        public static TConstructorDelegate GetConstructor<TConstructorDelegate>(this Type type) where TConstructorDelegate : Delegate
        {
            var ctor = TypeAccessor.CreateConstructor<TConstructorDelegate>();
            return ctor;
        }

        public static FastDynamicDelegate GetConstructorEx(this Type type, Type[] ctorParamTypes)
        {
            var ctor = TypeAccessor.GetConstructorEx(type, ctorParamTypes);
            return ctor;
        }

        public static object CreateInstance(this Type type, object[] ctorArgs)
        {
            if (ctorArgs == null || ctorArgs.Length == 0)
            {
                return type.CreateInstance();
            }

            var constructors = type.GetConstructors();
            ConstructorInfo targetCtor = null;
            foreach (var ctorInfo in constructors)
            {
                var parameters = ctorInfo.GetParameters();
                if (parameters.Length != ctorArgs.Length)
                {
                    continue;
                }

                bool match = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    var arg = ctorArgs[i];
                    var paramType = parameters[i].ParameterType;
                    if (arg == null)
                    {
                        if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                        {
                            match = false;
                            break;
                        }
                    }
                    else if (!paramType.IsAssignableFrom(arg.GetType()))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    targetCtor = ctorInfo;
                    break;
                }
            }

            if (targetCtor != null)
            {
                var invoker = TypeAccessor.GetConstructorEx(targetCtor);
                return invoker(ctorArgs);
            }

            var argTypes = ctorArgs.Select(a => a?.GetType() ?? typeof(object)).ToArray();
            var fallbackCtor = TypeAccessor.GetConstructorEx(type, argTypes);
            return fallbackCtor(ctorArgs);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> on which the method is invoked.
        /// </summary>
        /// <param name="type">The type on which the method was invoked.</param>
        /// <returns>An instance of the <paramref name="type"/>.</returns>
        public static object CreateInstance(this Type type)
        {
            var delegateType = TypeAccessor.GetFuncType([type]);
            var ctor = TypeAccessor.CreateConstructorEx(delegateType);
            return ctor();
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> on which the method is invoked.
        /// </summary>
        /// <typeparam name="TArg">The type of the argument to pass to the constructor.</typeparam>
        /// <param name="type">The type on which the method was invoked.</param>
        /// <param name="arg">The argument to pass to the constructor.</param>
        /// <returns>An instance of the given <paramref name="type"/>.</returns>
        public static object CreateInstance<TArg>(this Type type, TArg arg)
        {
            var delegateType = TypeAccessor.GetFuncType([typeof(TArg), type]);
            var ctor = TypeAccessor.CreateConstructorEx(delegateType);
            return ctor(arg);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> on which the method is invoked.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the constructor.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the constructor.</typeparam>
        /// <param name="type">The type on which the method was invoked.</param>
        /// <param name="arg1">The first argument to pass to the constructor.</param>
        /// <param name="arg2">The second argument to pass to the constructor.</param>
        /// <returns>An instance of the given <paramref name="type"/>.</returns>
        public static object CreateInstance<TArg1, TArg2>(this Type type, TArg1 arg1, TArg2 arg2)
        {
            var delegateType = TypeAccessor.GetFuncType([typeof(TArg1), typeof(TArg2), type]);
            var ctor = TypeAccessor.CreateConstructorEx(delegateType);
            return ctor(arg1, arg2);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> on which the method is invoked.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the constructor.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the constructor.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the constructor.</typeparam>
        /// <param name="type">The type on which the method was invoked.</param>
        /// <param name="arg1">The first argument to pass to the constructor.</param>
        /// <param name="arg2">The second argument to pass to the constructor.</param>
        /// <param name="arg3">The third argument to pass to the constructor.</param>
        /// <returns>An instance of the given <paramref name="type"/>.</returns>
        public static object CreateInstance<TArg1, TArg2, TArg3>(
            this Type type,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3)
        {
            var delegateType = TypeAccessor.GetFuncType([typeof(TArg1), typeof(TArg2), typeof(TArg3), type]);
            var ctor = TypeAccessor.CreateConstructorEx(delegateType);
            return ctor(arg1, arg2, arg3);
        }

        public static TDelegate GetStaticMethodCaller<TDelegate>(this Type type, string name)
        {
            return TypeAccessor.GetStaticMethodCaller<TDelegate>(type, name);
        }

        public static TDelegate GetMethodCaller<TDelegate>(this Type type, string name)
        {
            return TypeAccessor.GetMethodCaller<TDelegate>(type, name);
        }

        public static TDelegate GetPropertyGetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            var propInfo = type.GetProperty(name);
            return (TDelegate)TypeAccessor.GetPropertyGetter(propInfo);
        }

        public static TDelegate GetFieldGetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            var fieldInfo = type.GetField(name);
            return (TDelegate)TypeAccessor.GetFieldGetter(fieldInfo);
        }
    }
}
