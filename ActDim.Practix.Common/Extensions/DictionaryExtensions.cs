using Ardalis.GuardClauses;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
	/// <summary>
	/// <see cref="IDictionary"/> and <see cref="IDictionary<TKey, TValue>"/> Extensions
	/// </summary>
	public static class DictionaryExtensions //IEnumerableExtensions
	{
		/// <summary>
		/// Copies items from the other dictionary.
		/// </summary>
		/// <param name="destination">The destination.</param>
		/// <param name="source">The source.</param>
		/// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
		public static IDictionary CopyFrom(this IDictionary destination, IDictionary source, bool overwrite)
		{
			source.CopyTo(destination, overwrite);
			return destination;
		}

		/// <summary>
		/// Copies items to the other dictionary.
		/// </summary>		
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
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

		//GetOrCreate
		/// <summary>
		/// Gets or adds the item to dictionary by key and value.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		/// <param name="dictionary">The dictionary.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentNullException">key</exception>
		public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (!dictionary.TryGetValue(key, out TValue local))
			{
				local = value;
				dictionary.Add(key, local);
			}
			return local;
		}

		//GetOrCreate		
		/// <summary>
		/// Gets or adds the item provided by key and valueFactory to dictionary.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		/// <param name="dictionary">The dictionary.</param>
		/// <param name="key">The key.</param>
		/// <param name="valueFactory">The value factory.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentNullException">
		/// key
		/// or
		/// valueFactory
		/// </exception>
		public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory) //where TValue : class, new()
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			
            Guard.Against.Null(valueFactory, nameof(valueFactory));

            if (!dictionary.TryGetValue(key, out TValue value))
			{
				value = valueFactory(key);
				dictionary.Add(key, value);
			}
			return value;
		}
				
		// SLOW:
		// https://www.codeproject.com/Articles/724978/GetOrCreateValueDictionary
		//public static TV GetOrAddSafe<TK, TV>(this ConcurrentDictionary<TK, Lazy<TV>> dictionary, TK key, Func<TK, TV> creator)
		//{
		//	Lazy<TV> lazy = dictionary.GetOrAdd(key, new Lazy<TV>(() => creator(key)));
		//	return lazy.Value;
		//}

		//public static TV AddOrUpdateSafe<TK, TV>(this ConcurrentDictionary<TK, Lazy<TV>> dictionary, TK key, Func<TK, TV> creator, Func<TK, TV, TV> updater)
		//{
		//	Lazy<TV> lazy = dictionary.AddOrUpdate(key,
		//		new Lazy<TV>(() => creator(key)),
		//		(k, oldValue) => new Lazy<TV>(() => updater(k, oldValue.Value)));
		//	return lazy.Value;
		//}
	}
}
