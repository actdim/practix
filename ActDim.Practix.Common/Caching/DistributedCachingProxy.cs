using ActDim.Practix.Abstractions.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ActDim.Practix.Caching
{
    /// <inheritdoc />
    public class DistributedCachingProxy : IDistributedCachingProxy
    {
        private readonly IDistributedCache _cache;
        private readonly IBinarySerializer _serializer;

        public DistributedCachingProxy(IDistributedCache cache, IBinarySerializer serializer)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc />
        public Func<string, T> Get<T>(Func<string, T> func, DistributedCacheEntryOptions options)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var resultType = CachingProxyHelper.GetAwaitableResultType(typeof(T), out var isValueTask);
            if (resultType != null)
            {
                var builder = typeof(DistributedCachingProxy)
                    .GetMethod(isValueTask ? nameof(BuildValueTask) : nameof(BuildTask), BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(resultType);
                return (Func<string, T>)builder.Invoke(this, new object[] { func, options });
            }

            return key =>
            {
                var cached = _cache.Get(key);
                if (cached != null)
                {
                    return _serializer.Deserialize<T>(cached);
                }

                var value = func(key);
                _cache.Set(key, _serializer.Serialize(value), options);
                return value;
            };
        }

        private Func<string, Task<TResult>> BuildTask<TResult>(object funcObj, DistributedCacheEntryOptions options)
        {
            var func = (Func<string, Task<TResult>>)funcObj;
            return async key =>
            {
                var cached = await _cache.GetAsync(key).ConfigureAwait(false);
                if (cached != null)
                {
                    return _serializer.Deserialize<TResult>(cached);
                }

                var value = await func(key).ConfigureAwait(false);
                await _cache.SetAsync(key, _serializer.Serialize(value), options).ConfigureAwait(false);
                return value;
            };
        }

        private Func<string, ValueTask<TResult>> BuildValueTask<TResult>(object funcObj, DistributedCacheEntryOptions options)
        {
            var func = (Func<string, ValueTask<TResult>>)funcObj;
            return async key =>
            {
                var cached = await _cache.GetAsync(key).ConfigureAwait(false);
                if (cached != null)
                {
                    return _serializer.Deserialize<TResult>(cached);
                }

                var value = await func(key).ConfigureAwait(false);
                await _cache.SetAsync(key, _serializer.Serialize(value), options).ConfigureAwait(false);
                return value;
            };
        }
    }
}
