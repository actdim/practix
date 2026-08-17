using Ardalis.GuardClauses;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDictionary"/> and <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Copies key/value entries from the source dictionary into the destination dictionary.
        /// </summary>
        /// <param name="destination">The destination dictionary.</param>
        /// <param name="source">The source dictionary.</param>
        /// <param name="overwrite">Whether existing destination keys should be overwritten.</param>
        /// <returns>The destination dictionary.</returns>
        public static IDictionary CopyFrom(this IDictionary destination, IDictionary source, bool overwrite)
        {
            source.CopyTo(destination, overwrite);
            return destination;
        }

        /// <summary>
        /// Copies key/value entries to the target destination dictionary.
        /// </summary>
        private static IDictionary CopyTo(this IDictionary source, IDictionary destination, bool overwrite)
        {
            if (source != null)
            {
                foreach (DictionaryEntry entry in source)
                {
                    if (overwrite || !destination.Contains(entry.Key))
                    {
                        destination[entry.Key] = entry.Value;
                    }
                }
            }

            return source;
        }

        /// <summary>
        /// Gets the value associated with the specified key if present; otherwise, adds and returns the given value.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">The default value to insert on a miss.</param>
        /// <returns>The existing or newly added value.</returns>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            Guard.Against.Null(key, nameof(key));
            if (!dictionary.TryGetValue(key, out TValue local))
            {
                local = value;
                dictionary.Add(key, local);
            }

            return local;
        }

        /// <summary>
        /// Gets the value associated with the specified key if present; otherwise, invokes <paramref name="valueFactory"/> to add and return the new value.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="valueFactory">The factory function to produce a value on a miss.</param>
        /// <returns>The existing or newly added value.</returns>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        {
            Guard.Against.Null(key, nameof(key));
            Guard.Against.Null(valueFactory, nameof(valueFactory));

            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = valueFactory(key);
                dictionary.Add(key, value);
            }

            return value;
        }
    }
}
