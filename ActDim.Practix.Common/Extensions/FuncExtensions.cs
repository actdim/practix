using ActDim.Practix.Collections.Concurrent;
using System;
using System.Collections.Generic;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for function memoization and thread-safe function caching.
    /// </summary>
    public static class FuncExtensions
    {
        /// <summary>
        /// Memoizes a single-parameter function using thread-safe, exactly-once caching.
        /// </summary>
        /// <typeparam name="TArg">The input argument type.</typeparam>
        /// <typeparam name="TRetVal">The return value type.</typeparam>
        /// <param name="f">The function to memoize.</param>
        /// <returns>A thread-safe memoized delegate.</returns>
        public static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f)
            where TArg : notnull
        {
            ArgumentNullException.ThrowIfNull(f, nameof(f));
            var cache = new ConcurrentFactoryDictionary<TArg, TRetVal>(f);
            return arg => cache.GetOrCreateValue(arg);
        }

        /// <summary>
        /// Memoizes a single-parameter function with a custom equality comparer.
        /// </summary>
        /// <typeparam name="TArg">The input argument type.</typeparam>
        /// <typeparam name="TRetVal">The return value type.</typeparam>
        /// <param name="f">The function to memoize.</param>
        /// <param name="comparer">The equality comparer to use for caching keys.</param>
        /// <returns>A thread-safe memoized delegate.</returns>
        public static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f, IEqualityComparer<TArg> comparer)
            where TArg : notnull
        {
            ArgumentNullException.ThrowIfNull(f, nameof(f));
            ArgumentNullException.ThrowIfNull(comparer, nameof(comparer));
            var cache = new ConcurrentFactoryDictionary<TArg, TRetVal>(f, comparer);
            return arg => cache.GetOrCreateValue(arg);
        }
    }
}
