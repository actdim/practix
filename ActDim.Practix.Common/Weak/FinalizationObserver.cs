using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ActDim.Practix
{
	/// <summary>
	/// Finalizer
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FinalizationObserver<T> where T : class
	{
		/// <summary>
		/// Bind
		/// </summary>
		/// <param name="key"></param>
		/// <param name="handler"></param>
		public static void Subscribe(T key, Action<T> handler)
		{
			var finalizer = WeakTable.GetValue(key, k => new FinalizationObserver<T>() { _key = k });

			finalizer._handler += handler;
		}

		/// <summary>
		/// Unbind
		/// </summary>
		/// <param name="key"></param>
		/// <param name="handler"></param>
		public static void Unsubscribe(T key, Action<T> handler)
		{
			if (WeakTable.TryGetValue(key, out FinalizationObserver<T> finalizer))
			{
				finalizer._handler -= handler;
			}
		}

		~FinalizationObserver()
		{
			var handler = _handler;
			if (handler != null)
			{
				handler(_key);
			}
		}

		private event Action<T> _handler;
		private T _key;

		private static readonly ConditionalWeakTable<T, FinalizationObserver<T>> WeakTable = new ConditionalWeakTable<T, FinalizationObserver<T>>();
	}
}
