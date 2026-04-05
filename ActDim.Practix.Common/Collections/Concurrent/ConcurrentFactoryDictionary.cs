using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;

namespace ActDim.Practix.Collections.Concurrent
{
    public sealed class ConcurrentFactoryDictionary<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>
        where TKey : notnull

    {
        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _dictionary;
        private readonly Func<TKey, Lazy<TValue>> _valueFactory;
        // private readonly Type _valueType = typeof(TValue);

        public ConcurrentFactoryDictionary(Func<TKey, TValue> valueFactory)
        {
            Guard.Against.Null(valueFactory, nameof(valueFactory));
            _dictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
            _valueFactory = key => new Lazy<TValue>(() => valueFactory(key));
        }

        public TValue this[TKey key] { get => _dictionary[key].Value; }

        public IEnumerable<TKey> Keys => _dictionary.Keys;

        public IEnumerable<TValue> Values
        {
            get
            {
                // foreach (var kvp in _dictionary)
                // {
                //     yield return kvp.Value.Value;
                // }
                foreach (var value in _dictionary.Values)
                {
                    yield return value.Value;
                }
            }
        }

        public int Count => _dictionary.Count;

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable()
        {
            foreach (var kvp in _dictionary)
            {
                yield return KeyValuePair.Create(kvp.Key, kvp.Value.Value);
            }
        }

        /*
        public IEnumerable<(TKey, TValue)> AsEnumerable()
        {
            foreach (var kvp in _dictionary)
            {
                yield return (kvp.Key, kvp.Value.Value);
            }
        }
        */

        public TValue GetOrCreateValue(TKey key)
        {
            return _dictionary.GetOrAdd(key, _valueFactory).Value;
        }

        public void Remove(TKey key)
        {
            Lazy<TValue> container;
            var result = _dictionary.Remove(key, out container);
            // if (!result)
            // {
            //     throw new KeyNotFoundException();
            // }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            Lazy<TValue> container;
            var result = _dictionary.TryRemove(key, out container);
            value = result ? container.Value : default;
            return result;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            Lazy<TValue> container;
            var result = _dictionary.TryGetValue(key, out container);
            value = result ? container.Value : default;
            return result;
        }
    }
}
