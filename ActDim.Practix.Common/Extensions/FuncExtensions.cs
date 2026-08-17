using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class FuncExtensions
    {
        public class FactoryDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, IDisposable
        {
            private readonly ReaderWriterLockSlim _lock = new();

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

        private bool _isDisposed;

        /// <summary>
        /// Releases managed and unmanaged resources held by this instance.
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

        public static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f, FactoryDictionary<TArg, TRetVal> cache = null)
        {
            if (cache == null)
            {
                cache = new FactoryDictionary<TArg, TRetVal>();
            }
            return key => cache.GetOrAdd(key, f);
        }

        // private static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f, ConcurrentDictionary<TArg, Lazy<TRetVal>> cache = null)
        // {
        //     if (cache == null)
        //     {
        //         cache = new ConcurrentDictionary<TArg, Lazy<TRetVal>>();
        //     }
        //     return arg => cache.GetOrAdd(arg, new Lazy<TRetVal>(() => f(arg))).Value;
        // }

        public static Func<TArg, TRetVal> Memoize<TArg, TRetVal>(this Func<TArg, TRetVal> f, ConcurrentDictionary<TArg, TRetVal> cache = null)
        {
            if (cache == null)
            {
                cache = new ConcurrentDictionary<TArg, TRetVal>();
            }

            var syncMap = new ConcurrentDictionary<TArg, object>();
            return a =>
            {
                TRetVal r;
                if (!cache.TryGetValue(a, out r))
                {
                    var sync = syncMap.GetOrAdd(a, new object());
                    lock (sync)
                    {
                        r = cache.GetOrAdd(a, f);
                    }

                    syncMap.TryRemove(a, out sync);
                }

                return r;
            };
        }
    }
}
