using Microsoft.Extensions.Caching.Memory;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ActDim.Practix.Caching
{
    /// <inheritdoc />
    public class MemoryCachingProxy : IMemoryCachingProxy
    {
        private readonly IMemoryCache _cache;

        public MemoryCachingProxy(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc />
        public Func<string, T> Get<T>(Func<string, T> func, MemoryCacheEntryOptions options)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var resultType = CachingProxyHelper.GetAwaitableResultType(typeof(T), out var isValueTask);
            if (resultType != null)
            {
                var builder = typeof(MemoryCachingProxy)
                    .GetMethod(isValueTask ? nameof(BuildValueTask) : nameof(BuildTask), BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(resultType);
                return (Func<string, T>)builder.Invoke(this, new object[] { func, options });
            }

            return key =>
            {
                if (_cache.TryGetValue(key, out T cached))
                {
                    return cached;
                }

                var value = func(key);
                _cache.Set(key, value, options);
                return value;
            };
        }

        private Func<string, Task<TResult>> BuildTask<TResult>(object funcObj, MemoryCacheEntryOptions options)
        {
            var func = (Func<string, Task<TResult>>)funcObj;
            return async key =>
            {
                if (_cache.TryGetValue(key, out TResult cached))
                {
                    return cached;
                }

                var value = await func(key);
                _cache.Set(key, value, options);
                return value;
            };
        }

        private Func<string, ValueTask<TResult>> BuildValueTask<TResult>(object funcObj, MemoryCacheEntryOptions options)
        {
            var func = (Func<string, ValueTask<TResult>>)funcObj;
            return async key =>
            {
                if (_cache.TryGetValue(key, out TResult cached))
                {
                    return cached;
                }

                var value = await func(key);
                _cache.Set(key, value, options);
                return value;
            };
        }
    }
}
