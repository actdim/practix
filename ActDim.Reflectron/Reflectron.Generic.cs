using Ardalis.GuardClauses;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Reflectron
{
    /// <summary>
    /// Provides fast reflection-based member access and mutation for a specific object instance using weak references,
    /// as well as strongly-typed static reflection utilities for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public class Reflectron<T> : IReflectron<T> where T : class
    {
        private static readonly ConcurrentDictionary<(Type TargetType, string Name, Type ValueType), Delegate> _memberGetterCache =
            new ConcurrentDictionary<(Type, string, Type), Delegate>();

        private static readonly ConcurrentDictionary<(Type TargetType, string Name, Type ValueType), Delegate> _memberSetterCache =
            new ConcurrentDictionary<(Type, string, Type), Delegate>();

        private readonly WeakReference<T> _instanceWeakRef;
        private readonly Type _targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="Reflectron{T}"/> class wrapping the specified instance with a weak reference.
        /// </summary>
        /// <param name="instance">The target instance.</param>
        public Reflectron(T instance) : this(instance, typeof(T))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Reflectron{T}"/> class wrapping the specified instance with a target type.
        /// </summary>
        /// <param name="instance">The target instance.</param>
        /// <param name="targetType">The explicit target type for member lookup.</param>
        public Reflectron(T instance, Type targetType)
        {
            Guard.Against.Null(instance, nameof(instance));
            Guard.Against.Null(targetType, nameof(targetType));
            _instanceWeakRef = new WeakReference<T>(instance);
            _targetType = targetType;
        }

        private T TargetInstance
        {
            get
            {
                if (_instanceWeakRef.TryGetTarget(out var instance))
                {
                    return instance;
                }

                throw new ReflectionException("Can't access target object");
            }
        }

        /// <inheritdoc />
        public object this[string name]
        {
            get
            {
                Guard.Against.NullOrEmpty(name, nameof(name));
                return Get<object>(name);
            }
            set
            {
                Guard.Against.NullOrEmpty(name, nameof(name));
                Set(name, value);
            }
        }

        /// <inheritdoc />
        public TMember Get<TMember>(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            var getter = GetMemberGetter<TMember>(name);
            return getter(TargetInstance);
        }

        /// <inheritdoc />
        public TMember Get<TMember>(Expression<Func<T, TMember>> memberExpr)
        {
            Guard.Against.Null(memberExpr, nameof(memberExpr));
            var memberInfo = Reflectron.GetMemberInfo(memberExpr);
            if (memberInfo is PropertyInfo propInfo)
            {
                var getter = Reflectron.GetPropertyGetter<T, TMember>(propInfo);
                return getter(TargetInstance);
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                var getter = Reflectron.GetFieldGetter<T, TMember>(fieldInfo);
                return getter(TargetInstance);
            }

            throw new ArgumentException($"Member '{memberInfo.Name}' is neither a property nor a field.", nameof(memberExpr));
        }

        /// <inheritdoc />
        public TMember Set<TMember>(string name, TMember value)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            var setter = GetMemberSetter<TMember>(name);
            setter(TargetInstance, value);
            return value;
        }

        /// <inheritdoc />
        public TMember Set<TMember>(Expression<Func<T, TMember>> memberExpr, TMember value)
        {
            Guard.Against.Null(memberExpr, nameof(memberExpr));
            var memberInfo = Reflectron.GetMemberInfo(memberExpr);
            if (memberInfo is PropertyInfo propInfo)
            {
                var setter = Reflectron.GetPropertySetter<T, TMember>(propInfo);
                setter(TargetInstance, value);
                return value;
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                var setter = Reflectron.GetFieldSetter<T, TMember>(fieldInfo);
                setter(TargetInstance, value);
                return value;
            }

            throw new ArgumentException($"Member '{memberInfo.Name}' is neither a property nor a field.", nameof(memberExpr));
        }

        /// <inheritdoc />
        public TDelegate GetMethod<TDelegate>(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            return Reflectron.GetMethodCaller<TDelegate>(_targetType, name);
        }

        /// <inheritdoc />
        public TDelegate GetMethod<TDelegate>(Expression<Action<T>> methodExpr)
        {
            Guard.Against.Null(methodExpr, nameof(methodExpr));
            return GetMethod<TDelegate>((LambdaExpression)methodExpr);
        }

        /// <inheritdoc />
        public TDelegate GetMethod<TDelegate, TResult>(Expression<Func<T, TResult>> methodExpr)
        {
            Guard.Against.Null(methodExpr, nameof(methodExpr));
            return GetMethod<TDelegate>((LambdaExpression)methodExpr);
        }

        /// <inheritdoc />
        public TDelegate GetMethod<TDelegate>(LambdaExpression methodExpr)
        {
            Guard.Against.Null(methodExpr, nameof(methodExpr));
            var memberInfo = Reflectron.GetMemberInfo(methodExpr);
            if (memberInfo is MethodInfo methodInfo)
            {
                return Reflectron.GetMethodCaller<TDelegate>(methodInfo);
            }

            throw new ArgumentException($"Member '{memberInfo.Name}' is not a method.", nameof(methodExpr));
        }

        private Func<T, TMember> GetMemberGetter<TMember>(string name)
        {
            var key = (_targetType, name, typeof(TMember));
            return (Func<T, TMember>)_memberGetterCache.GetOrAdd(key, k =>
            {
                var propInfo = k.TargetType.GetProperty(k.Name);
                if (propInfo != null)
                {
                    return Reflectron.GetPropertyGetter<T, TMember>(propInfo);
                }

                var fieldInfo = k.TargetType.GetField(k.Name);
                if (fieldInfo != null)
                {
                    return Reflectron.GetFieldGetter<T, TMember>(fieldInfo);
                }

                throw new ArgumentException($"Member '{k.Name}' not found on type '{k.TargetType.FullName}'.", nameof(name));
            });
        }

        private Action<T, TMember> GetMemberSetter<TMember>(string name)
        {
            var key = (_targetType, name, typeof(TMember));
            return (Action<T, TMember>)_memberSetterCache.GetOrAdd(key, k =>
            {
                var propInfo = k.TargetType.GetProperty(k.Name);
                if (propInfo != null)
                {
                    return Reflectron.GetPropertySetter<T, TMember>(propInfo);
                }

                var fieldInfo = k.TargetType.GetField(k.Name);
                if (fieldInfo != null)
                {
                    return Reflectron.GetFieldSetter<T, TMember>(fieldInfo);
                }

                throw new ArgumentException($"Member '{k.Name}' not found on type '{k.TargetType.FullName}'.", nameof(name));
            });
        }

        #region Static Helpers for Type T

        public static MemberInfo GetMemberInfo<TOutput>(Expression<Func<T, TOutput>> expr)
        {
            return Reflectron.GetMemberInfo(expr);
        }

        public static FieldInfo GetFieldInfo<TField>(Expression<Func<T, TField>> expr)
        {
            return Reflectron.GetFieldInfo(expr);
        }

        public static PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<T, TProperty>> expr)
        {
            return Reflectron.GetPropertyInfo(expr);
        }

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

        public static MethodInfo GetMethodInfo<TOutput>(Expression<Func<T, TOutput>> expr)
        {
            return Reflectron.GetMethodInfo(expr);
        }

        public static Func<T, TProperty> GetPropertyGetter<TProperty>(PropertyInfo propInfo)
        {
            return Reflectron.GetPropertyGetter<T, TProperty>(propInfo);
        }

        public static Func<T, TField> GetFieldGetter<TField>(FieldInfo fieldInfo)
        {
            return Reflectron.GetFieldGetter<T, TField>(fieldInfo);
        }

        public static Func<T, TProperty> GetPropertyGetter<TProperty>(string name)
        {
            return Reflectron.GetPropertyGetter<T, TProperty>(name);
        }

        public static Func<T, TProperty> GetPropertyGetter<TProperty>(Expression<Func<T, TProperty>> expr)
        {
            return Reflectron.GetPropertyGetter(expr);
        }

        public static Action<T, TProperty> GetPropertySetter<TProperty>(PropertyInfo propInfo)
        {
            return Reflectron.GetPropertySetter<T, TProperty>(propInfo);
        }

        public static Action<T, TProperty> GetPropertySetter<TProperty>(string name)
        {
            return Reflectron.GetPropertySetter<T, TProperty>(name);
        }

        public static Action<T, TProperty> GetPropertySetter<TProperty>(Expression<Func<T, TProperty>> expr)
        {
            return Reflectron.GetPropertySetter(expr);
        }

        public static TProperty GetProperty<TProperty>(T obj, string name)
        {
            return GetPropertyGetter<TProperty>(name)(obj);
        }

        public static TProperty GetProperty<TProperty>(T obj, Expression<Func<T, TProperty>> expr)
        {
            return GetPropertyGetter(expr)(obj);
        }

        public static TProperty SetProperty<TProperty>(T obj, string name, TProperty value)
        {
            GetPropertySetter<TProperty>(name)(obj, value);
            return value;
        }

        public static TProperty SetProperty<TProperty>(T obj, Expression<Func<T, TProperty>> expr, TProperty value)
        {
            GetPropertySetter(expr)(obj, value);
            return value;
        }

        public static Func<T, TField> GetFieldGetter<TField>(string name)
        {
            return Reflectron.GetFieldGetter<T, TField>(name);
        }

        public static Action<T, TField> GetFieldSetter<TField>(FieldInfo fieldInfo)
        {
            return Reflectron.GetFieldSetter<T, TField>(fieldInfo);
        }

        public static Action<T, TField> GetFieldSetter<TField>(string name)
        {
            return Reflectron.GetFieldSetter<T, TField>(name);
        }

        public static TField GetField<TField>(T obj, string name)
        {
            return GetFieldGetter<TField>(name)(obj);
        }

        public static TField SetField<TField>(T obj, string name, TField value)
        {
            GetFieldSetter<TField>(name)(obj, value);
            return value;
        }

        public static TDelegate GetStaticMethodCaller<TDelegate>(string name)
        {
            return Reflectron.GetStaticMethodCaller<T, TDelegate>(name);
        }

        public static TDelegate GetMethodCaller<TDelegate>(string name)
        {
            return Reflectron.GetMethodCaller<T, TDelegate>(name);
        }

        #endregion
    }
}
