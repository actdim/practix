using ActDim.Practix.TypeAccess.Reflection;
using Ardalis.GuardClauses;
using System;

namespace ActDim.Practix.TypeAccess.Linq
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Creates an <see cref="IObjectAccessor{T}"/> wrapper for the target object.
        /// </summary>
        public static IObjectAccessor<T> GetAccessor<T>(this T obj) where T : class
        {
            return new ObjectAccessor<T>(obj);
        }

        public static TDelegate GetPropertyGetter<TDelegate>(this object obj, string name) where TDelegate : Delegate
        {
            Guard.Against.Null(obj, nameof(obj));
            return obj.GetType().GetPropertyGetter<TDelegate>(name);
        }

        public static TDelegate GetFieldGetter<TDelegate>(this object obj, string name) where TDelegate : Delegate
        {
            Guard.Against.Null(obj, nameof(obj));
            return obj.GetType().GetFieldGetter<TDelegate>(name);
        }

        public static TProperty GetProperty<TProperty>(this object obj, string name)
        {
            Guard.Against.Null(obj, nameof(obj));
            Guard.Against.NullOrEmpty(name, nameof(name));
            var propInfo = obj.GetType().GetProperty(name);
            Guard.Against.Null(propInfo, nameof(name), $"Property '{name}' not found on type '{obj.GetType().FullName}'.");
            var getter = TypeAccessor.GetPropertyGetter<object, TProperty>(propInfo);
            return getter(obj);
        }

        public static TField GetField<TField>(this object obj, string name)
        {
            Guard.Against.Null(obj, nameof(obj));
            Guard.Against.NullOrEmpty(name, nameof(name));
            var fieldInfo = obj.GetType().GetField(name);
            Guard.Against.Null(fieldInfo, nameof(name), $"Field '{name}' not found on type '{obj.GetType().FullName}'.");
            var getter = TypeAccessor.GetFieldGetter<object, TField>(fieldInfo);
            return getter(obj);
        }

        public static TDelegate GetMethodCaller<TDelegate>(this object obj, string name)
        {
            Guard.Against.Null(obj, nameof(obj));
            return obj.GetType().GetMethodCaller<TDelegate>(name);
        }
    }
}
