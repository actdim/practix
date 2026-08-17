using ActDim.Practix.Common;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for general object instances.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Indicates whether the specified object reference is null.
        /// </summary>
        /// <param name="obj">The object reference to test.</param>
        /// <returns>True if null; otherwise, false.</returns>
        public static bool IsNull(this object obj)
        {
            return obj is null;
        }

        /// <summary>
        /// Determines whether a value is equal to the default value of its type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value to test.</param>
        /// <returns>True if equal to default(T); otherwise, false.</returns>
        public static bool IsDefault<T>(this T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default);
        }

        /// <summary>
        /// Wraps a single element into an <see cref="IEnumerable{T}"/> sequence containing that single item.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="item">The single item.</param>
        /// <returns>An enumerable containing the single item.</returns>
        internal static IEnumerable<T> Enumerate<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Returns the string representation of an object, or <see cref="string.Empty"/> if null or <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The string representation or empty string.</returns>
        public static string ToStringSafe(this object obj)
        {
            return obj != null && obj != DBNull.Value ? obj.ToString() : string.Empty;
        }

        /// <summary>
        /// Extracts the member name from a lambda expression evaluated on the target object.
        /// </summary>
        /// <param name="target">The target object instance.</param>
        /// <param name="expression">The expression referencing the target member.</param>
        /// <returns>The string name of the member.</returns>
        public static string NameOf(this object target, Expression<Func<object>> expression)
        {
            return NameHelper.NameOf(expression);
        }

        /// <summary>
        /// Extracts the member name from a typed lambda expression evaluated on the target object.
        /// </summary>
        /// <typeparam name="T">The expression return type.</typeparam>
        /// <param name="target">The target object instance.</param>
        /// <param name="expression">The expression referencing the target member.</param>
        /// <returns>The string name of the member.</returns>
        public static string NameOf<T>(this object target, Expression<Func<T>> expression)
        {
            return NameHelper.NameOf(expression);
        }
    }
}
