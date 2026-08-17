using Microsoft.Extensions.Caching.Memory;
using System;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IMemoryCache"/> providing atomic add-or-get-existing operations.
    /// </summary>
    public static partial class MemoryCacheExtensions
    {
        /// <summary>Improved version of IMemoryCache "add or get existing" behavior.</summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="cache">The cache to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>A TValue.</returns>
        public static TValue AddOrGetExisting<TValue>(this IMemoryCache cache, object key, TValue value)
        {
            return cache.AddOrGetExisting(key, _ => value, (MemoryCacheEntryOptions)null);
        }

        /// <summary>Improved version of IMemoryCache "add or get existing" behavior.</summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="cache">The cache to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueFactory">The value factory.</param>
        /// <returns>A TValue.</returns>
        public static TValue AddOrGetExisting<TValue>(this IMemoryCache cache, object key, Func<object, TValue> valueFactory)
        {
            return cache.AddOrGetExisting(key, valueFactory, (MemoryCacheEntryOptions)null);
        }

        /// <summary>Improved version of IMemoryCache "add or get existing" behavior.</summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="cache">The cache to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueFactory">The value factory.</param>
        /// <param name="options">The cache entry options.</param>
        /// <returns>A TValue.</returns>
        public static TValue AddOrGetExisting<TValue>(this IMemoryCache cache, object key, Func<object, TValue> valueFactory, MemoryCacheEntryOptions options)
        {
            if (cache.TryGetValue(key, out object existing))
            {
                return ((Lazy<TValue>)existing).Value;
            }

            var lazy = new Lazy<TValue>(() => valueFactory(key));

            using (ICacheEntry entry = cache.CreateEntry(key))
            {
                if (options != null)
                {
                    entry.SetOptions(options);
                }

                entry.Value = lazy;
            }

            return cache.TryGetValue(key, out object winner)
                ? ((Lazy<TValue>)winner).Value
                : lazy.Value;
        }

        /// <summary>Improved version of IMemoryCache "add or get existing" behavior.</summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="cache">The cache to act on.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueFactory">The value factory.</param>
        /// <param name="absoluteExpiration">The absolute expiration.</param>
        /// <returns>A TValue.</returns>
        public static TValue AddOrGetExisting<TValue>(this IMemoryCache cache, object key, Func<object, TValue> valueFactory, DateTimeOffset absoluteExpiration)
        {
            return cache.AddOrGetExisting(key, valueFactory, new MemoryCacheEntryOptions { AbsoluteExpiration = absoluteExpiration });
        }
    }
}
