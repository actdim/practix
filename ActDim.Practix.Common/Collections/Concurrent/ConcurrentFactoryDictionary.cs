using Ardalis.GuardClauses;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ActDim.Practix.Collections.Concurrent
{
    /// <summary>
    /// A thread-safe dictionary backed by <see cref="ConcurrentDictionary{TKey, TValue}"/> and <see cref="Lazy{T}"/>,
    /// guaranteeing thread-safe, exactly-once creation of cached values via a factory delegate.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public sealed class ConcurrentFactoryDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _dictionary;
        private readonly Func<TKey, Lazy<TValue>> _valueFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class using the specified value factory.
        /// </summary>
        /// <param name="valueFactory">The factory delegate used to generate values for missing keys.</param>
        public ConcurrentFactoryDictionary(Func<TKey, TValue> valueFactory)
        {
            Guard.Against.Null(valueFactory, nameof(valueFactory));
            _dictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
            _valueFactory = key => new Lazy<TValue>(() => valueFactory(key), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public TValue this[TKey key] => _dictionary[key].Value;

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public IEnumerable<TKey> Keys => _dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var value in _dictionary.Values)
                {
                    yield return value.Value;
                }
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerable sequence of key/value pairs in the dictionary.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable()
        {
            foreach (var kvp in _dictionary)
            {
                yield return KeyValuePair.Create(kvp.Key, kvp.Value.Value);
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key if it exists, or creates and caches it using the value factory.
        /// </summary>
        /// <param name="key">The key of the value to get or create.</param>
        /// <returns>The cached or created value.</returns>
        public TValue GetOrCreateValue(TKey key)
        {
            var lazy = _dictionary.GetOrAdd(key, _valueFactory);
            try
            {
                return lazy.Value;
            }
            catch
            {
                // Remove failed lazy instance so subsequent attempts can retry
                _dictionary.TryRemove(KeyValuePair.Create(key, lazy));
                throw;
            }
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(TKey key)
        {
            _dictionary.TryRemove(key, out _);
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary and returns it if removed.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">When this method returns, contains the removed value if found; otherwise, the default value.</param>
        /// <returns><c>true</c> if the element was successfully removed; otherwise, <c>false</c>.</returns>
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_dictionary.TryRemove(key, out var container))
            {
                value = container.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key if present in the dictionary.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if found; otherwise, the default value.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_dictionary.TryGetValue(key, out var container))
            {
                value = container.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var kvp in _dictionary)
            {
                yield return KeyValuePair.Create(kvp.Key, kvp.Value.Value);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
