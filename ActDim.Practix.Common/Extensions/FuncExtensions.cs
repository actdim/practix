using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for function memoization and thread-safe function caching.
    /// </summary>
    public static class FuncExtensions
    {
        /// <summary>
        /// A thread-safe dictionary that synchronizes value factory executions.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        public class FactoryDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, IDisposable
        {
            private readonly ReaderWriterLockSlim _lock = new();
            private bool _isDisposed;

            /// <summary>
            /// Gets or adds a key/value pair to the dictionary using a synchronized value factory.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="valueFactory">The function used to generate a value for the key.</param>
            /// <returns>The value for the key.</returns>
            public new TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            {
                TValue result;

                _lock.EnterWriteLock();
                try
                {
                    result = base.GetOrAdd(key, valueFactory);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                return result;
            }

            /// <summary>
            /// Releases managed resources held by this instance.
            /// </summary>
            /// <param name="disposing">true if called from <see cref="Dispose()"/>; false from finalizer.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        _lock.Dispose();
                    }

                    _isDisposed = true;
                }
            }

            /// <summary>
            /// Finalizer — calls <see cref="Dispose(bool)"/> with <c>false</c>.
            /// </summary>
            ~FactoryDictionary()
            {
                Dispose(false);
            }

            /// <summary>
            /// Releases all resources used by this instance.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Memoizes a single-parameter function using a <see cref="FactoryDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TArg">The input argument type.</typeparam>
        /// <typeparam name="TRetVal">The return value type.</typeparam>
        /// <param name="f">The function to memoize.</param>
        /// <param name="cache">An optional pre-existing cache dictionary.</param>
        /// <returns>A memoized delegate.</returns>
        public static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f, FactoryDictionary<TArg, TRetVal> cache = null)
        {
            if (cache == null)
            {
                cache = new FactoryDictionary<TArg, TRetVal>();
            }

            return key => cache.GetOrAdd(key, f);
        }

        /// <summary>
        /// Memoizes a single-parameter function using a <see cref="ConcurrentDictionary{TKey, TValue}"/> with per-key synchronization.
        /// </summary>
        /// <typeparam name="TArg">The input argument type.</typeparam>
        /// <typeparam name="TRetVal">The return value type.</typeparam>
        /// <param name="f">The function to memoize.</param>
        /// <param name="cache">An optional pre-existing concurrent cache dictionary.</param>
        /// <returns>A memoized delegate.</returns>
        public static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f, ConcurrentDictionary<TArg, TRetVal> cache = null)
        {
            if (cache == null)
            {
                cache = new ConcurrentDictionary<TArg, TRetVal>();
            }

            var syncMap = new ConcurrentDictionary<TArg, object>();
            return a =>
            {
                if (!cache.TryGetValue(a, out var r))
                {
                    var sync = syncMap.GetOrAdd(a, new object());
                    lock (sync)
                    {
                        r = cache.GetOrAdd(a, f);
                    }

                    syncMap.TryRemove(a, out _);
                }

                return r;
            };
        }
    }
}
