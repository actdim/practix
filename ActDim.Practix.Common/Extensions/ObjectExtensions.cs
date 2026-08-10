using ActDim.Practix.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    // target - source/value/instance
    public static class ObjectExtensions
    {
        /// <summary>
        /// Determines if the object is null
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if it is null, false otherwise</returns>
        public static bool IsNull(this object obj)
        {
            return obj is null;
        }

        public static bool IsDefault<T>(this T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default);
        }

        /// <summary>
        /// Turns a single item into an enumerable
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="item">Item</param>
        /// <returns>Enumerable containing the single item</returns>
        internal static IEnumerable<T> Enumerate<T>(this T item) //EnumerateOne
        {
            yield return item;
        }

        /// <summary>
        /// Gets the safe string representation of an object which is the ToString() result for non-null objects and String.Empty otherwise
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public static string ToStringSafe(this object obj)
        {
            return (obj != null && obj != DBNull.Value ? obj.ToString() : string.Empty);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string NameOf(this object target, Expression<Func<object>> expression) //source/instance
        {
            return NameHelper.NameOf(expression);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string NameOf<T>(this object target, Expression<Func<T>> expression) //source/instance
        {
            return NameHelper.NameOf(expression);
        }
    }
}
