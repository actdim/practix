using ActDim.Practix.TypeAccess.Reflection;

namespace ActDim.Practix.TypeAccess.Linq // ActDim.Practix.TypeAccess.Extensions
{
    public static class TypeExtensions
    {
        // private delegate T ObjectActivator<T>(params object[] args);

        public static TConstructorDelegate GetConstructor<TConstructorDelegate>(this Type type) where TConstructorDelegate : Delegate
        {
            var ctor = TypeAccessor.CreateConstructor<TConstructorDelegate>();
            return ctor;
        }

        public static FastDynamicDelegate GetConstructorEx(this Type type, Type[] ctorParamTypes) // GetCtorEx
        {
            var ctor = TypeAccessor.GetConstructorEx(type, ctorParamTypes);
            return ctor;
        }

        public static object CreateInstance(this Type type, object[] ctorArgs) // Construct
        {
            var ctor = TypeAccessor.GetConstructorEx(type, ctorArgs.Select(a => a.GetType()).ToArray());
            return ctor(ctorArgs);
        }

        /// <summary>
        /// Creates an instance of the <paramref name="type"/> on which the method is invoked.
        /// </summary>
        /// <param name="type">The type on which the method was invoked.</param>
        /// <returns>An instance of the <paramref name="type"/>.</returns>
        public static object CreateInstance(this Type type) // New/NewInstance/Construct/Instantiate
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
            // var ctor = TypeAccessor.GetConstructor(type, [typeof(TArg)]);
            // return ctor.DynamicInvoke(arg);
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
            // var ctor = TypeAccessor.GetConstructor(type, [typeof(TArg1), typeof(TArg2)]);
            // return ctor.DynamicInvoke(arg1, arg2);
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
            // var ctor = TypeAccessor.GetConstructor(type, [typeof(TArg1), typeof(TArg2), typeof(TArg3)]);
            // return ctor.DynamicInvoke(arg1, arg2, arg3);
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
            return TypeAccessor.GetPropertyGetter(type, name) as TDelegate;
        }

        public static TDelegate GetFieldGetter<TDelegate>(this Type type, string name) where TDelegate : Delegate
        {
            return TypeAccessor.GetFieldGetter(type, name) as TDelegate;
        }
    }
}
