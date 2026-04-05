using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Caching;

namespace ActDim.Practix.Caching
{
	public abstract class CachingProxyFactoryBase
	{
		protected static ProxyGenerator ProxyGenerator = new ProxyGenerator(false);
	}

	/// <summary>
	/// MemoizationProxy Factory/Provider
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CachingProxyFactory<T> : CachingProxyFactoryBase, ICachingProxyFactory<T> where T : class
	{
		private const string MethodGroupExpressionExpectedErrorMessage = @"Method ""group"" expression expected";

		public IMemoryCache MemoryCache { get; set; }

		public IDistributedCache DistributedCache { get; set; }

		private readonly IJsonSerializer _jsonSerializer;

		private readonly IDictionary<MethodInfo, MemoryCacheEntryOptions> _memoryCacheEntryOptionMap;

		private readonly IDictionary<MethodInfo, DistributedCacheEntryOptions> _distributedCacheEntryOptionMap;

		private readonly IDictionary<MethodInfo, InvocationContextConfig> _configMap;

		private readonly IInvocationContextSerializer _invocationContextSerializer;				

		public CachingProxyFactory(IMemoryCache memoryCache, IDistributedCache distributedCache, IJsonSerializer jsonSerializer, IInvocationContextSerializer invocationContextSerializer)
		{
			MemoryCache = memoryCache;
			DistributedCache = distributedCache;
			_jsonSerializer = jsonSerializer;
			_memoryCacheEntryOptionMap = new ConcurrentDictionary<MethodInfo, MemoryCacheEntryOptions>();
			_distributedCacheEntryOptionMap = new ConcurrentDictionary<MethodInfo, DistributedCacheEntryOptions>();
			_configMap = new ConcurrentDictionary<MethodInfo, InvocationContextConfig>();
			_invocationContextSerializer = invocationContextSerializer;
		}

		public T Create(T obj, IInvocationContextSerializer invocationContextSerializer) //, IJsonSerializer jsonSerializer
		{
			if (!typeof(T).IsInterface)
			{
				// type parameter
				throw new ArgumentException($"An interface expected as generic argument"); // {nameof(T)}
			}
			// jsonSerializer ?? _jsonSerializer
			return ProxyGenerator.CreateInterfaceProxyWithTarget(obj, new CachingInterceptor<T>(MemoryCache, DistributedCache, _jsonSerializer, _memoryCacheEntryOptionMap, _distributedCacheEntryOptionMap, _configMap, invocationContextSerializer ?? _invocationContextSerializer));
		}

		public T Create(T obj)
		{
			return Create(obj, _invocationContextSerializer);
		}        

        /// <summary>
        /// ExtractMethodInfo
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private MethodInfo GetMethodInfo<TDelegate>(Expression<Func<T, TDelegate>> expression)
		{
			MethodInfo result = null;

			if (expression.Body is UnaryExpression unaryExpression)
			{
				if (unaryExpression.Operand is MethodCallExpression methodCallExpression)
				{
					if (methodCallExpression.Object is ConstantExpression constantExpression)
					{
						result = constantExpression.Value as MethodInfo;
					}
				}
			}
			return result;
		}		

		public ICachingProxyFactory<T> ConfigureMethod<TDelegate>(Expression<Func<T, TDelegate>> expression, MemoryCacheEntryOptions memoryCacheEntryOptions, InvocationContextConfig config)
		{
			var mi = GetMethodInfo(expression);
			if (mi == null)
			{
				throw new ArgumentException(MethodGroupExpressionExpectedErrorMessage, nameof(expression));
			}
			_memoryCacheEntryOptionMap[mi] = memoryCacheEntryOptions;
			_distributedCacheEntryOptionMap.Remove(mi);
			_configMap[mi] = config;
			return this;
		}

		public ICachingProxyFactory<T> ConfigureMethod<TDelegate>(Expression<Func<T, TDelegate>> expression, DistributedCacheEntryOptions distributedCacheEntryOptions, InvocationContextConfig config)
		{
			var mi = GetMethodInfo(expression);
			if (mi == null)
			{
				throw new ArgumentException(MethodGroupExpressionExpectedErrorMessage, nameof(expression));
			}
			_distributedCacheEntryOptionMap[mi] = distributedCacheEntryOptions;
			_memoryCacheEntryOptionMap.Remove(mi);
			_configMap[mi] = config;
			return this;
		}		
	}

	// TODO: implement
    public class Memoizer //: IMemoizer
    {        
        public IMemoryCache MemoryCache { get; set; }

        public IDistributedCache DistributedCache { get; set; }

        private readonly IJsonSerializer _jsonSerializer;
     
        public Memoizer(IMemoryCache memoryCache, IDistributedCache distributedCache, IJsonSerializer jsonSerializer)
        {
            MemoryCache = memoryCache;
            DistributedCache = distributedCache;
            _jsonSerializer = jsonSerializer;
        }

        public TDelegate Create<TDelegate>(Expression<TDelegate> expression, MemoryCacheEntryOptions memoryCacheEntryOptions)
        {
            return expression.Compile();
        }

        public TDelegate Create<TDelegate>(Expression<TDelegate> expression, DistributedCacheEntryOptions distributedCacheEntryOptions)
        {
            return expression.Compile();
        }
    }
}
