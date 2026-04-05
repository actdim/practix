using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using SalientBits.InterCode.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using CanarySystems.Abstractions.Serialization;
using System.Runtime.Serialization;

namespace CanarySystems.Caching
{
    internal class CachingInterceptor<T> : IInterceptor // where T : class
    {
        // private static readonly MethodInfo HandleAsyncMethodInfo = typeof(CachingInterceptor<T>).GetMethod(nameof(HandleAsyncWithResult), BindingFlags.Static | BindingFlags.NonPublic);		

        private readonly Type _instanceType;

        private readonly IMemoryCache _memoryCache;

        private readonly IDistributedCache _distributedCache;

        private readonly IJsonSerializer _jsonSerializer;

        private readonly IDictionary<MethodInfo, MemoryCacheEntryOptions> _memoryCacheEntryOptionMap;

        private readonly IDictionary<MethodInfo, DistributedCacheEntryOptions> _distributedCacheEntryOptionMap;

        /// <summary>
        /// invocation context config map
        /// </summary>
        private readonly IDictionary<MethodInfo, InvocationContextConfig> _configMap;

        private readonly IInvocationContextSerializer _invocationContextSerializer;

        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);

        // private static ConcurrentDictionary<string, Lazy<SemaphoreSlim>> LockMap = new ConcurrentDictionary<string, Lazy<SemaphoreSlim>>();
        private static IDictionary<string, SemaphoreSlim> LockMap = new Dictionary<string, SemaphoreSlim>();

        public CachingInterceptor(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IJsonSerializer jsonSerializer,
            IDictionary<MethodInfo, MemoryCacheEntryOptions> memoryCacheEntryOptionMap,
            IDictionary<MethodInfo, DistributedCacheEntryOptions> distributedCacheEntryOptions,
            IDictionary<MethodInfo, InvocationContextConfig> configMap,
            IInvocationContextSerializer invocationContextSerializer
            )
        {
            _instanceType = typeof(T);
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _jsonSerializer = jsonSerializer;

            _memoryCacheEntryOptionMap = memoryCacheEntryOptionMap;
            if (_memoryCacheEntryOptionMap == null)
            {
                _memoryCacheEntryOptionMap = new Dictionary<MethodInfo, MemoryCacheEntryOptions>();
            }

            _distributedCacheEntryOptionMap = distributedCacheEntryOptions;
            if (_distributedCacheEntryOptionMap == null)
            {
                _distributedCacheEntryOptionMap = new Dictionary<MethodInfo, DistributedCacheEntryOptions>();
            }

            _configMap = configMap;
            if (_configMap == null)
            {
                _configMap = new Dictionary<MethodInfo, InvocationContextConfig>();
            }

            _invocationContextSerializer = invocationContextSerializer;

            LoadAttributeConfiguration();
        }

        private void LoadAttributeConfiguration()
        {
            // _instanceType.IsInterface?			
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            var methods = _instanceType.GetMethods(bindingFlags);

            foreach (var mi in methods)
            {
                var attribute = (CacheAttribute)mi.GetCustomAttributes(typeof(CacheAttribute), false).LastOrDefault();

                if (attribute != null)
                {
                    var attributeConfig = new InvocationContextConfig()
                    {
                        ExcludeParameterIndexes = attribute.ExcludeParameterIndexes,
                        ExcludeParameterTypes = attribute.ExcludeParameterTypes,
                        InvocationContextSerializerType = attribute.InvocationContextSerializerType,
                        Tag = attribute.Tag
                    };

                    if (_configMap.TryGetValue(mi, out InvocationContextConfig config) && config != null)
                    {
                        if (string.IsNullOrEmpty(config.Tag))
                        {
                            config.Tag = attributeConfig.Tag;
                        }

                        if (config.ExcludeParameterIndexes == null || config.ExcludeParameterIndexes.Length == 0)
                        {
                            config.ExcludeParameterIndexes = attributeConfig.ExcludeParameterIndexes;
                        }

                        if (config.ExcludeParameterTypes == null || config.ExcludeParameterTypes.Length == 0)
                        {
                            config.ExcludeParameterTypes = attributeConfig.ExcludeParameterTypes;
                        }
                    }
                    else
                    {
                        config = attributeConfig;
                    }

                    CacheType? cacheType = null;

                    if (_memoryCacheEntryOptionMap.TryGetValue(mi, out MemoryCacheEntryOptions memoryCacheEntryOptions))
                    {
                        cacheType = CacheType.InMemory;
                    }

                    if (_distributedCacheEntryOptionMap.TryGetValue(mi, out DistributedCacheEntryOptions distributedCacheEntryOptions))
                    {
                        cacheType = CacheType.Distributed;
                    }

                    if (attribute.CacheType == CacheType.InMemory)
                    {
                        if (cacheType == null || cacheType == CacheType.InMemory)
                        {
                            if (memoryCacheEntryOptions == null)
                            {
                                memoryCacheEntryOptions = new MemoryCacheEntryOptions();
                            }
                            if (!string.IsNullOrEmpty(attribute.AbsoluteExpiration))
                            {
                                memoryCacheEntryOptions.AbsoluteExpiration = DateTimeOffset.Parse(attribute.AbsoluteExpiration);
                            }
                            if (!string.IsNullOrEmpty(attribute.AbsoluteExpirationRelativeToNow))
                            {
                                memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.Parse(attribute.AbsoluteExpirationRelativeToNow);
                            }
                            if (!string.IsNullOrEmpty(attribute.SlidingExpiration))
                            {
                                memoryCacheEntryOptions.SlidingExpiration = TimeSpan.Parse(attribute.SlidingExpiration);
                            }
                        }
                        _memoryCacheEntryOptionMap[mi] = memoryCacheEntryOptions;
                    }
                    else if (attribute.CacheType == CacheType.Distributed)
                    {
                        if (cacheType == null || cacheType == CacheType.Distributed)
                        {
                            if (distributedCacheEntryOptions == null)
                            {
                                distributedCacheEntryOptions = new DistributedCacheEntryOptions();
                            }
                            if (!string.IsNullOrEmpty(attribute.AbsoluteExpiration))
                            {
                                distributedCacheEntryOptions.AbsoluteExpiration = DateTimeOffset.Parse(attribute.AbsoluteExpiration);
                            }
                            if (!string.IsNullOrEmpty(attribute.AbsoluteExpirationRelativeToNow))
                            {
                                distributedCacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.Parse(attribute.AbsoluteExpirationRelativeToNow);
                            }
                            if (!string.IsNullOrEmpty(attribute.SlidingExpiration))
                            {
                                distributedCacheEntryOptions.SlidingExpiration = TimeSpan.Parse(attribute.SlidingExpiration);
                            }
                        }
                        _distributedCacheEntryOptionMap[mi] = distributedCacheEntryOptions;
                    }

                    _configMap[mi] = config;
                }
            }
        }

        public void Intercept(IInvocation invocation)
        {
            MemoryCacheEntryOptions memoryCacheEntryOptions = null;
            DistributedCacheEntryOptions distributedCacheEntryOptions = null;
            if (!_memoryCacheEntryOptionMap.TryGetValue(invocation.Method, out memoryCacheEntryOptions))
                _distributedCacheEntryOptionMap.TryGetValue(invocation.Method, out distributedCacheEntryOptions);

            if (memoryCacheEntryOptions != null || distributedCacheEntryOptions != null)
            {
                var method = invocation.Method;

                _configMap.TryGetValue(method, out InvocationContextConfig config);

                var invocationKey = _invocationContextSerializer.Serialize(method, config, invocation.Arguments);

                var delegateType = GetDelegateType(invocation.MethodInvocationTarget);

                if (delegateType == MethodType.AsyncAction)
                {
                    // invocation.MethodInvocationTarget
                    // method call/invocation
                    throw new InvalidOperationException($"Can't cache method without result (\"{method.Name}\")"); // ...with void result
                }

                var returnType = method.ReturnType;
                if (delegateType == MethodType.AsyncFunction)
                {
                    returnType = returnType.GetGenericArguments()[0];
                }

                var isCached = false;
                object returnValue = null;

                SemaphoreSlim invocationLock = null;

                _syncRoot.Wait(); // TODO: use finite timeout for safety

                try
                {
                    if (memoryCacheEntryOptions != null)
                    {
                        isCached = _memoryCache.TryGetValue(invocationKey, out returnValue);
                    }
                    else
                    {
                        // data
                        var bytes = _distributedCache.Get(invocationKey);
                        isCached = bytes != null;
                        if (isCached)
                        {
                            if (returnType == typeof(byte[]))
                            {
                                returnValue = bytes;
                            }
                            else
                            {
                                var entryValueType = typeof(ValueContainer<>).MakeGenericType(returnType); // valueContainerType
                                var entryValue = _jsonSerializer.DeserializeObjectFromBson(bytes, entryValueType); // valueContainer

                                // var propertyInfo = TypeAccessor.GetProperty(() => default(IContainer).Value);
                                var propertyInfo = TypeAccessor.GetProperty((IContainer c) => c.Value);
                                var valueGetter = TypeAccessor.GetPropertyGetter(propertyInfo);
                                returnValue = valueGetter.DynamicInvoke(entryValue);
                            }

                        }
                    }
                    // we do not use ConcurrentDictionary
                    // we need to use locking with SyncRoot here since ConcurrentDictionary does not guarantee single call of value factory during GetOrAdd

                    if (!LockMap.TryGetValue(invocationKey, out invocationLock))
                    {
                        invocationLock = new SemaphoreSlim(1, 1);
                        LockMap.Add(invocationKey, invocationLock);
                    }
                }
                finally
                {
                    _syncRoot.Release();
                }

                if (isCached)
                {
                    if (delegateType == MethodType.AsyncFunction)
                    {
                        invocation.ReturnValue = Task.FromResult((dynamic)returnValue);
                    }
                    else
                    {
                        invocation.ReturnValue = returnValue;
                    }
                }
                else
                {
                    invocationLock.Wait();

                    // Monitor.Enter(invocationLock);

                    var action = new Action<object>(result =>
                    {
                        if (memoryCacheEntryOptions != null)
                        {
                            _memoryCache.Set(invocationKey, result, memoryCacheEntryOptions);
                        }
                        else
                        {
                            // data
                            byte[] bytes = null;
                            if (returnType == typeof(byte[]))
                            {
                                bytes = (byte[])result;
                            }
                            else
                            {
                                var entryValueType = typeof(ValueContainer<>).MakeGenericType(returnType); // valueContainerType
                                var entryValueCtor = TypeAccessor.GetConstructor(entryValueType, returnType); // valueContainerCtor
                                var entryValue = entryValueCtor(result);
                                bytes = _jsonSerializer.SerializeObjectToBson(entryValue);
                            }

                            _distributedCache.Set(invocationKey, bytes, distributedCacheEntryOptions);
                        }
                    });

                    if (delegateType == MethodType.Synchronous)
                    {
                        try
                        {
                            invocation.Proceed();
                        }
                        finally
                        {
                            invocationLock.Release();
                        }

                    }
                    else if (delegateType == MethodType.AsyncFunction)
                    {
                        try
                        {
                            invocation.Proceed();

                            invocation.ReturnValue =
                                HandleAsyncWithResult((dynamic)invocation.ReturnValue, (dynamic)action);
                        }
                        finally
                        {
                            invocationLock.Release();
                        }
                    }
                }
            }
            else
            {
                invocation.Proceed();
            }

            if (invocation.ReturnValue == invocation.InvocationTarget)
            {
                invocation.ReturnValue = invocation.Proxy;
            }
        }

        // private object ExecuteHandleAsyncWithResult(IInvocation invocation, Action<object> continuationAction)
        // {
        // 	var resultType = invocation.Method.ReturnType.GetGenericArguments()[0];
        // 	var mi = HandleAsyncMethodInfo.MakeGenericMethod(resultType);
        // 	var caller = TypeAccessor.GetMethodCaller(mi);
        // 	return caller(this, new[] { invocation.ReturnValue, continuationAction });
        // }

        private static async Task<TResult> HandleAsyncWithResult<TResult>(Task<TResult> task, Action<object> continuationAction)
        {
            var result = await task.ConfigureAwait(false);
            continuationAction(result);
            return result;
        }

        private MethodType GetDelegateType(MethodInfo method)
        {
            var returnType = method.ReturnType;
            var isAsync = method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;

            if (isAsync)
            {
                // typeof(Task).IsAssignableFrom(method.ReturnType)
                if (returnType == typeof(Task))
                {
                    return MethodType.AsyncAction;
                }
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    return MethodType.AsyncFunction;
                }
            }
            return MethodType.Synchronous;
        }

        private enum MethodType
        {
            Synchronous,
            AsyncAction,
            AsyncFunction
        }
    }

    internal interface IContainer
    {
        object Value { get; set; }
    }

    /// <summary>
    /// Distributed cache entry value container
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    internal class ValueContainer<T> : IContainer
    {
        [Obfuscation(Exclude = true)]
        [DataMember]
        public T Value { get; set; }
        object IContainer.Value { get => Value; set => Value = (T)value; }

        public ValueContainer()
        {

        }

        public ValueContainer(T value)
        {
            Value = value;
        }
    }
}