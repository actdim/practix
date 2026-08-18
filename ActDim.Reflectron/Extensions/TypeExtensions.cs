using Ardalis.GuardClauses;
using System;
using System.Linq;
using System.Reflection;

namespace ActDim.Reflectron
{
    /// <summary>
    /// Extension methods for <see cref="Type"/> to obtain fast compiled constructor, method, property, and field delegates.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns a factory function that creates <see cref="IReflectron{T}"/> instances for objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <param name="type">The type to reflect.</param>
        /// <returns>A factory delegate that creates an <see cref="IReflectron{T}"/> for a given instance.</returns>
        public static Func<T, IReflectron<T>> Reflect<T>(this Type type) where T : class
        {
            Ardalis.GuardClauses.Guard.Against.Null(type, nameof(type));
            return instance => new Reflectron<T>(instance);
        }

        /// <summary>
        /// Returns a factory function that creates <see cref="IReflectron{Object}"/> instances for runtime objects of the specified type.
        /// </summary>
        /// <param name="type">The runtime type to reflect.</param>
        /// <returns>A factory delegate that creates an <see cref="IReflectron{Object}"/> for a given instance.</returns>
        public static Func<object, IReflectron<object>> Reflect(this Type type)
        {
            Ardalis.GuardClauses.Guard.Against.Null(type, nameof(type));
            return instance => new Reflectron<object>(instance, type);
        }

        /// <summary>
        /// Gets a compiled constructor delegate matching <typeparamref name="TConstructorDelegate"/>.
        /// </summary>
        /// <typeparam name="TConstructorDelegate">The delegate type whose signature matches the target constructor.</typeparam>
        /// <param name="type">The type to construct.</param>
        /// <returns>A compiled constructor delegate.</returns>
        public static TConstructorDelegate GetConstructor<TConstructorDelegate>(this Type type) where TConstructorDelegate : Delegate
        {
            var ctor = Reflectron.CreateConstructor<TConstructorDelegate>();
            return ctor;
        }

        /// <summary>
        /// Gets an untyped fast dynamic constructor delegate taking constructor parameter types.
        /// </summary>
        /// <param name="type">The type to construct.</param>
        /// <param name="ctorParamTypes">The parameter types of the target constructor.</param>
        /// <returns>A <see cref="FastDynamicDelegate"/> that invokes the constructor.</returns>
        public static FastDynamicDelegate GetConstructorEx(this Type type, Type[] ctorParamTypes)
        {
            var ctor = Reflectron.GetConstructorEx(type, ctorParamTypes);
            return ctor;
        }

        /// <summary>
        /// Creates an instance of <paramref name="type"/> passing <paramref name="ctorArgs"/> to a matching constructor.
        /// </summary>
        /// <param name="type">The target type to instantiate.</param>
        /// <param name="ctorArgs">Arguments passed to the constructor.</param>
        /// <returns>The newly created object instance.</returns>
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
                var invoker = Reflectron.GetConstructorEx(targetCtor);
                return invoker(ctorArgs);
            }

            var argTypes = ctorArgs.Select(a => a?.GetType() ?? typeof(object)).ToArray();
            var fallbackCtor = Reflectron.GetConstructorEx(type, argTypes);
            return fallbackCtor(ctorArgs);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> using its parameterless constructor.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>An instance of the <paramref name="type"/>.</returns>
        public static object CreateInstance(this Type type)
        {
            var delegateType = Reflectron.GetFuncType([type]);
            var ctor = Reflectron.CreateConstructorEx(delegateType);
            return ctor();
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> passing one argument to the constructor.
        /// </summary>
        /// <typeparam name="TArg">The type of the argument to pass to the constructor.</typeparam>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="arg">The argument to pass to the constructor.</param>
        /// <returns>An instance of the given <paramref name="type"/>.</returns>
        public static object CreateInstance<TArg>(this Type type, TArg arg)
        {
            var delegateType = Reflectron.GetFuncType([typeof(TArg), type]);
            var ctor = Reflectron.CreateConstructorEx(delegateType);
            return ctor(arg);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> passing two arguments to the constructor.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument.</typeparam>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>An instance of the given <paramref name="type"/>.</returns>
        public static object CreateInstance<TArg1, TArg2>(this Type type, TArg1 arg1, TArg2 arg2)
        {
            var delegateType = Reflectron.GetFuncType([typeof(TArg1), typeof(TArg2), type]);
            var ctor = Reflectron.CreateConstructorEx(delegateType);
            return ctor(arg1, arg2);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> passing three arguments to the constructor.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument.</typeparam>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>An instance of the given <paramref name="type"/>.</returns>
        public static object CreateInstance<TArg1, TArg2, TArg3>(
            this Type type,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3)
        {
            var delegateType = Reflectron.GetFuncType([typeof(TArg1), typeof(TArg2), typeof(TArg3), type]);
            var ctor = Reflectron.CreateConstructorEx(delegateType);
            return ctor(arg1, arg2, arg3);
        }

        /// <summary>
        /// Gets a compiled static method caller delegate for the specified method name on <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the static method signature.</typeparam>
        /// <param name="type">The target type.</param>
        /// <param name="name">The static method name.</param>
        /// <returns>A compiled static method caller delegate.</returns>
        public static TDelegate GetStaticMethodCaller<TDelegate>(this Type type, string name)
        {
            return Reflectron.GetStaticMethodCaller<TDelegate>(type, name);
        }

        /// <summary>
        /// Gets a compiled instance method caller delegate for the specified method name on <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the method signature (with instance as 1st parameter).</typeparam>
        /// <param name="type">The target type.</param>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static TDelegate GetMethodCaller<TDelegate>(this Type type, string name)
        {
            return Reflectron.GetMethodCaller<TDelegate>(type, name);
        }

        /// <summary>
        /// Gets a compiled property getter delegate for the specified property name on <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The getter delegate type.</typeparam>
        /// <param name="type">The target type.</param>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled property getter delegate.</returns>
        public static TDelegate GetPropertyGetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            var propInfo = type.GetProperty(name);
            return (TDelegate)Reflectron.GetPropertyGetter(propInfo);
        }

        /// <summary>
        /// Gets a compiled property setter delegate for the specified property name on <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The setter delegate type.</typeparam>
        /// <param name="type">The target type.</param>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled property setter delegate.</returns>
        public static TDelegate GetPropertySetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            var propInfo = type.GetProperty(name);
            return (TDelegate)Reflectron.GetPropertySetter(propInfo);
        }

        /// <summary>
        /// Gets a compiled field getter delegate for the specified field name on <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The getter delegate type.</typeparam>
        /// <param name="type">The target type.</param>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static TDelegate GetFieldGetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            var fieldInfo = type.GetField(name);
            return (TDelegate)Reflectron.GetFieldGetter(fieldInfo);
        }

        /// <summary>
        /// Gets a compiled field setter delegate for the specified field name on <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="TDelegate">The setter delegate type.</typeparam>
        /// <param name="type">The target type.</param>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field setter delegate.</returns>
        public static TDelegate GetFieldSetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            var fieldInfo = type.GetField(name);
            return (TDelegate)Reflectron.GetFieldSetter(fieldInfo);
        }
    }
}
