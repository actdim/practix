using ActDim.Practix.Collections.Concurrent;
using Ardalis.GuardClauses;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Reflectron
{
    public static partial class Reflectron
    {
        private static readonly Func<(Type, PropertyInfo), Delegate> GetTypedPropertyGetterDelegate = GetPropertyGetter;
        private static readonly ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate> TypedPropertyGetterCache =
            new ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate>(GetTypedPropertyGetterDelegate);

        private static readonly Func<(Type, PropertyInfo), Delegate> GetTypedPropertySetterDelegate = GetPropertySetter;
        private static readonly ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate> TypedPropertySetterCache =
            new ConcurrentFactoryDictionary<(Type, PropertyInfo), Delegate>(GetTypedPropertySetterDelegate);

        /// <summary>
        /// Gets a compiled delegate to read values from the given property.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TOutput">The property output type.</typeparam>
        /// <param name="propInfo">The property info.</param>
        /// <returns>A compiled property getter delegate.</returns>
        public static Func<TInstance, TOutput> GetPropertyGetter<TInstance, TOutput>(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));
            var pair = (typeof(Func<TInstance, TOutput>), propInfo);
            return (Func<TInstance, TOutput>)TypedPropertyGetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets an untyped delegate to read values from the given property.
        /// </summary>
        /// <param name="propInfo">The property info.</param>
        /// <returns>A compiled property getter delegate.</returns>
        public static Delegate GetPropertyGetter(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));
            var pair = (typeof(Delegate), propInfo);
            return TypedPropertyGetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a property getter delegate for the property on the specified type by name.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled property getter delegate.</returns>
        public static Delegate GetPropertyGetter(Type type, string name)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.NullOrEmpty(name, nameof(name));

            var propInfo = type.GetProperty(name);
            Guard.Against.Null(propInfo, nameof(name), $"Property '{name}' not found on type '{type.FullName}'.");
            return GetPropertyGetter(propInfo);
        }

        /// <summary>
        /// Gets a strongly-typed property getter delegate for the property on <typeparamref name="T"/> by name.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TProperty">The property value type.</typeparam>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled property getter delegate.</returns>
        public static Func<T, TProperty> GetPropertyGetter<T, TProperty>(string name)
        {
            return (Func<T, TProperty>)GetPropertyGetter(typeof(T), name);
        }

        /// <summary>
        /// Gets a strongly-typed property getter delegate from a member expression.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TProperty">The property value type.</typeparam>
        /// <param name="expr">The property access expression.</param>
        /// <returns>A compiled property getter delegate.</returns>
        public static Func<T, TProperty> GetPropertyGetter<T, TProperty>(Expression<Func<T, TProperty>> expr)
        {
            Guard.Against.Null(expr, nameof(expr));
            var propInfo = GetPropertyInfo(expr);
            return GetPropertyGetter<T, TProperty>(propInfo);
        }

        /// <summary>
        /// Gets a delegate that can be used to set the value of the given property.
        /// </summary>
        /// <param name="propInfo">The property info.</param>
        /// <returns>A compiled property setter delegate.</returns>
        public static Delegate GetPropertySetter(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));
            var pair = (typeof(Delegate), propInfo);
            return TypedPropertySetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a strongly-typed delegate to set values on the given property.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="propInfo">The property info.</param>
        /// <returns>A compiled property setter delegate.</returns>
        public static Action<TInstance, TValue> GetPropertySetter<TInstance, TValue>(PropertyInfo propInfo)
        {
            Guard.Against.Null(propInfo, nameof(propInfo));
            var pair = (typeof(Action<TInstance, TValue>), propInfo);
            return (Action<TInstance, TValue>)TypedPropertySetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a property setter delegate for the property on the specified type by name.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled property setter delegate.</returns>
        public static Delegate GetPropertySetter(Type type, string name)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.NullOrEmpty(name, nameof(name));

            var propInfo = type.GetProperty(name);
            Guard.Against.Null(propInfo, nameof(name), $"Property '{name}' not found on type '{type.FullName}'.");
            return GetPropertySetter(propInfo);
        }

        /// <summary>
        /// Gets a strongly-typed property setter delegate for the property on <typeparamref name="T"/> by name.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TProperty">The property value type.</typeparam>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled property setter delegate.</returns>
        public static Action<T, TProperty> GetPropertySetter<T, TProperty>(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            var propInfo = typeof(T).GetProperty(name);
            Guard.Against.Null(propInfo, nameof(name), $"Property '{name}' not found on type '{typeof(T).FullName}'.");
            return GetPropertySetter<T, TProperty>(propInfo);
        }

        /// <summary>
        /// Gets a strongly-typed property setter delegate from a member expression.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TProperty">The property value type.</typeparam>
        /// <param name="expr">The property access expression.</param>
        /// <returns>A compiled property setter delegate.</returns>
        public static Action<T, TProperty> GetPropertySetter<T, TProperty>(Expression<Func<T, TProperty>> expr)
        {
            Guard.Against.Null(expr, nameof(expr));
            var propInfo = GetPropertyInfo(expr);
            return GetPropertySetter<T, TProperty>(propInfo);
        }

        private static Delegate GetPropertyGetter((Type, PropertyInfo) pair)
        {
            var delegateType = pair.Item1;
            var propInfo = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments();
            var declaringType = propInfo.ReflectedType ?? propInfo.DeclaringType;
            var instanceType = delegateGenericArgs.Length == 2
                ? delegateGenericArgs[0]
                : (declaringType != null && (!declaringType.IsAbstract || !declaringType.IsSealed) ? declaringType : ObjectType);
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

            return lambdaExpr.Compile();
        }

        private static Delegate GetPropertySetter((Type, PropertyInfo) pair)
        {
            var delegateType = pair.Item1;
            var propInfo = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments();
            var instanceType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[0] : propInfo.ReflectedType ?? propInfo.DeclaringType;
            var valueType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[1] : propInfo.PropertyType;

            var instanceParamExpr = Expression.Parameter(instanceType, "instance");
            var valueParamExpr = Expression.Parameter(valueType, "value");

            Expression readValueParamExpr = valueParamExpr;
            if (propInfo.PropertyType != valueType)
            {
                readValueParamExpr = Expression.Convert(valueParamExpr, propInfo.PropertyType);
            }

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

            return lambdaExpr.Compile();
        }
    }
}
