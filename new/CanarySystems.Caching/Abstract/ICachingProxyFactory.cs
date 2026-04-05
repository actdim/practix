using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq.Expressions;

namespace CanarySystems.Caching
{
    // IFactory<T, T>, IFactory<T, T, IInvocationContextSerializer> 
    // IFactory<object, object>?
    public interface ICachingProxyFactory<T> where T : class
    {
        T Create(T obj);

        T Create(T obj, IInvocationContextSerializer invocationContextSerializer);

        ICachingProxyFactory<T> ConfigureMethod<TDelegate>(Expression<Func<T, TDelegate>> expression, MemoryCacheEntryOptions memoryCacheEntryOptions, InvocationContextConfig config);

        ICachingProxyFactory<T> ConfigureMethod<TDelegate>(Expression<Func<T, TDelegate>> expression, DistributedCacheEntryOptions distributedCacheEntryOptions, InvocationContextConfig config);

        IMemoryCache MemoryCache { get; } // GetMemoryCache

        IDistributedCache DistributedCache { get; } // GetDistributedCache
    }
}