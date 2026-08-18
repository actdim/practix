using System;

namespace ActDim.Reflectron
{
    /// <summary>
    /// Central entry point for high-performance reflection, expression-tree caching, and fluent object reflector creation.
    /// </summary>
    public static partial class Reflectron
    {
        /// <summary>
        /// Creates an <see cref="IReflectron{T}"/> instance wrapping the specified target object with a weak reference.
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <param name="instance">The target object instance.</param>
        /// <returns>An <see cref="IReflectron{T}"/> instance.</returns>
        public static IReflectron<T> For<T>(T instance) where T : class
        {
            return new Reflectron<T>(instance);
        }

        /// <summary>
        /// Creates an <see cref="IReflectron{Object}"/> instance wrapping the specified target object with an explicit runtime type.
        /// </summary>
        /// <param name="instance">The target object instance.</param>
        /// <param name="targetType">The runtime type for member lookup.</param>
        /// <returns>An <see cref="IReflectron{Object}"/> instance.</returns>
        public static IReflectron<object> For(object instance, Type targetType)
        {
            return new Reflectron<object>(instance, targetType);
        }
    }
}
