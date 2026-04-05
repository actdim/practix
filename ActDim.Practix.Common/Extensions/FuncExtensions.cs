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

            #region IDisposable Support
            private bool isDisposed = false; // To detect redundant calls

            /// <summary>
            /// 
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (!isDisposed)
                {
                    if (disposing)
                    {
                        // _lock.Dispose();
                    }
                    if (_lock != null)
                    {
                        _lock.Dispose();
                    }

                    isDisposed = true;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            ~FactoryDictionary()
            {
                Dispose(false);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion
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
