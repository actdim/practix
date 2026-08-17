using Ardalis.GuardClauses;
using System;

namespace ActDim.Reflectron
{
    /// <summary>
    /// Extension methods providing fast reflection access directly on object instances.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Creates an <see cref="IObjectAccess{T}"/> wrapper for the target object instance.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <returns>An <see cref="IObjectAccess{T}"/> instance wrapping the target object.</returns>
        public static IObjectAccess<T> GetAccessor<T>(this T obj) where T : class
        {
            return new ObjectAccess<T>(obj);
        }

        /// <summary>
        /// Gets a compiled property getter delegate for the specified property on the target object.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the property getter signature.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <param name="name">The property name.</param>
        /// <returns>A compiled getter delegate.</returns>
        public static TDelegate GetPropertyGetter<TDelegate>(this object obj, string name) where TDelegate : Delegate
        {
            Guard.Against.Null(obj, nameof(obj));
            return obj.GetType().GetPropertyGetter<TDelegate>(name);
        }

        /// <summary>
        /// Gets a compiled field getter delegate for the specified field on the target object.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the field getter signature.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <param name="name">The field name.</param>
        /// <returns>A compiled field getter delegate.</returns>
        public static TDelegate GetFieldGetter<TDelegate>(this object obj, string name) where TDelegate : Delegate
        {
            Guard.Against.Null(obj, nameof(obj));
            return obj.GetType().GetFieldGetter<TDelegate>(name);
        }

        /// <summary>
        /// Evaluates and returns the value of the specified property on the target object.
        /// </summary>
        /// <typeparam name="TProperty">The expected property return type.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The property value.</returns>
        public static TProperty GetProperty<TProperty>(this object obj, string name)
        {
            Guard.Against.Null(obj, nameof(obj));
            Guard.Against.NullOrEmpty(name, nameof(name));
            var propInfo = obj.GetType().GetProperty(name);
            Guard.Against.Null(propInfo, nameof(name), $"Property '{name}' not found on type '{obj.GetType().FullName}'.");
            var getter = TypeAccess.GetPropertyGetter<object, TProperty>(propInfo);
            return getter(obj);
        }

        /// <summary>
        /// Evaluates and returns the value of the specified field on the target object.
        /// </summary>
        /// <typeparam name="TField">The expected field return type.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <param name="name">The field name.</param>
        /// <returns>The field value.</returns>
        public static TField GetField<TField>(this object obj, string name)
        {
            Guard.Against.Null(obj, nameof(obj));
            Guard.Against.NullOrEmpty(name, nameof(name));
            var fieldInfo = obj.GetType().GetField(name);
            Guard.Against.Null(fieldInfo, nameof(name), $"Field '{name}' not found on type '{obj.GetType().FullName}'.");
            var getter = TypeAccess.GetFieldGetter<object, TField>(fieldInfo);
            return getter(obj);
        }

        /// <summary>
        /// Gets a compiled method caller delegate for the specified method on the target object.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the method invocation signature.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        public static TDelegate GetMethodCaller<TDelegate>(this object obj, string name)
        {
            Guard.Against.Null(obj, nameof(obj));
            return obj.GetType().GetMethodCaller<TDelegate>(name);
        }
    }
}
