using Ardalis.GuardClauses;

namespace ActDim.Reflectron
{
    /// <summary>
    /// Extension methods providing reflection access via <see cref="IReflectron{T}"/>.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Creates an <see cref="IReflectron{T}"/> wrapper for the target object instance.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <returns>An <see cref="IReflectron{T}"/> instance wrapping the target object.</returns>
        public static IReflectron<T> Reflectron<T>(this T obj) where T : class
        {
            Guard.Against.Null(obj, nameof(obj));
            return new Reflectron<T>(obj);
        }

        /// <summary>
        /// Creates an <see cref="IReflectron{T}"/> wrapper for the target object instance.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="obj">The target object instance.</param>
        /// <returns>An <see cref="IReflectron{T}"/> instance wrapping the target object.</returns>
        public static IReflectron<T> Reflect<T>(this T obj) where T : class
        {
            Guard.Against.Null(obj, nameof(obj));
            return new Reflectron<T>(obj);
        }
    }
}
