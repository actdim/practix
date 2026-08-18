using ActDim.Practix.Collections.Concurrent;
using Ardalis.GuardClauses;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Reflectron
{
    public static partial class Reflectron
    {
        private static readonly Func<(Type, FieldInfo), Delegate> GetTypedFieldGetterDelegate = GetFieldGetter;
        private static readonly ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate> TypedFieldGetterCache =
            new ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate>(GetTypedFieldGetterDelegate);

        private static readonly Func<(Type, FieldInfo), Delegate> GetTypedFieldSetterDelegate = GetFieldSetter;
        private static readonly ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate> TypedFieldSetterCache =
            new ConcurrentFactoryDictionary<(Type, FieldInfo), Delegate>(GetTypedFieldSetterDelegate);

        /// <summary>
        /// Gets an untyped compiled delegate to read values from the given field.
        /// </summary>
        /// <param name="fieldInfo">The field info.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static Delegate GetFieldGetter(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));
            var pair = (typeof(Delegate), fieldInfo);
            return TypedFieldGetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a strongly-typed compiled delegate to read values from the given field.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="fieldInfo">The field info.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static Func<T, TField> GetFieldGetter<T, TField>(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));
            var pair = (typeof(Func<T, TField>), fieldInfo);
            return (Func<T, TField>)TypedFieldGetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a field getter delegate for the field on the specified type by name.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static Delegate GetFieldGetter(Type type, string name)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.NullOrEmpty(name, nameof(name));

            var fieldInfo = type.GetField(name);
            Guard.Against.Null(fieldInfo, nameof(name), $"Field '{name}' not found on type '{type.FullName}'.");
            return GetFieldGetter(fieldInfo);
        }

        /// <summary>
        /// Gets a strongly-typed field getter delegate for the field on <typeparamref name="T"/> by name.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static Func<T, TField> GetFieldGetter<T, TField>(string name)
        {
            return (Func<T, TField>)GetFieldGetter(typeof(T), name);
        }

        /// <summary>
        /// Gets a strongly-typed field getter delegate from a member expression.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="expr">The field access expression.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static Func<T, TField> GetFieldGetter<T, TField>(Expression<Func<T, TField>> expr)
        {
            Guard.Against.Null(expr, nameof(expr));
            var fieldInfo = GetFieldInfo(expr);
            return GetFieldGetter<T, TField>(fieldInfo);
        }

        /// <summary>
        /// Gets an untyped compiled delegate to write values to the given field.
        /// </summary>
        /// <param name="fieldInfo">The field info.</param>
        /// <returns>A compiled field setter delegate.</returns>
        public static Delegate GetFieldSetter(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));
            var pair = (typeof(Delegate), fieldInfo);
            return TypedFieldSetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a strongly-typed compiled delegate to write values to the given field.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="fieldInfo">The field info.</param>
        /// <returns>A compiled field setter delegate.</returns>
        public static Action<T, TField> GetFieldSetter<T, TField>(FieldInfo fieldInfo)
        {
            Guard.Against.Null(fieldInfo, nameof(fieldInfo));
            var pair = (typeof(Action<T, TField>), fieldInfo);
            return (Action<T, TField>)TypedFieldSetterCache.GetOrCreateValue(pair);
        }

        /// <summary>
        /// Gets a field setter delegate for the field on the specified type by name.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field setter delegate.</returns>
        public static Delegate GetFieldSetter(Type type, string name)
        {
            Guard.Against.Null(type, nameof(type));
            Guard.Against.NullOrEmpty(name, nameof(name));

            var fieldInfo = type.GetField(name);
            Guard.Against.Null(fieldInfo, nameof(name), $"Field '{name}' not found on type '{type.FullName}'.");
            return GetFieldSetter(fieldInfo);
        }

        /// <summary>
        /// Gets a strongly-typed field setter delegate for the field on <typeparamref name="T"/> by name.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field setter delegate.</returns>
        public static Action<T, TField> GetFieldSetter<T, TField>(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            var fieldInfo = typeof(T).GetField(name);
            Guard.Against.Null(fieldInfo, nameof(name), $"Field '{name}' not found on type '{typeof(T).FullName}'.");
            return GetFieldSetter<T, TField>(fieldInfo);
        }

        /// <summary>
        /// Gets a strongly-typed field setter delegate from a member expression.
        /// </summary>
        /// <typeparam name="T">The declaring type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="expr">The field access expression.</param>
        /// <returns>A compiled field setter delegate.</returns>
        public static Action<T, TField> GetFieldSetter<T, TField>(Expression<Func<T, TField>> expr)
        {
            Guard.Against.Null(expr, nameof(expr));
            var fieldInfo = GetFieldInfo(expr);
            return GetFieldSetter<T, TField>(fieldInfo);
        }

        private static Delegate GetFieldGetter((Type, FieldInfo) pair)
        {
            var delegateType = pair.Item1;
            var field = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments();
            var instanceType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[0] : field.ReflectedType ?? field.DeclaringType;
            var resultType = delegateGenericArgs.Length == 2 ? delegateGenericArgs[1] : field.FieldType;

            var paramExpr = Expression.Parameter(instanceType, "instance");
            Expression resultExpr;

            if (field.IsStatic)
            {
                resultExpr = Expression.MakeMemberAccess(null, field);
            }
            else
            {
                Expression readParamExpr = paramExpr;
                if (field.DeclaringType != instanceType)
                {
                    readParamExpr = Expression.Convert(paramExpr, field.DeclaringType);
                }
                resultExpr = Expression.MakeMemberAccess(readParamExpr, field);
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

            return lambdaExpr.Compile();
        }

        private static Delegate GetFieldSetter((Type, FieldInfo) pair)
        {
            var delegateType = pair.Item1;
            var fieldInfo = pair.Item2;

            var delegateGenericArgs = delegateType.GetGenericArguments();
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
                accessFieldExpr = Expression.MakeMemberAccess(null, fieldInfo);
            }
            else
            {
                Expression readInstanceParamExpr = instanceParamExpr;
                if (fieldInfo.DeclaringType != instanceType)
                {
                    readInstanceParamExpr = Expression.Convert(instanceParamExpr, fieldInfo.DeclaringType);
                }
                accessFieldExpr = Expression.MakeMemberAccess(readInstanceParamExpr, fieldInfo);
            }

            var assignExpr = Expression.Assign(accessFieldExpr, readValueParamExpr);

            LambdaExpression lambdaExpr;
            if (delegateType == DelegateType)
            {
                lambdaExpr = Expression.Lambda(assignExpr, instanceParamExpr, valParamExpr);
            }
            else
            {
                lambdaExpr = Expression.Lambda(delegateType, assignExpr, instanceParamExpr, valParamExpr);
            }

            return lambdaExpr.Compile();
        }
    }
}
