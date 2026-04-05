using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CanarySystems.Abstractions.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using SalientBits.InterCode.Reflection;

namespace CanarySystems.Caching
{
    public class CacheFilterAttribute : Attribute, IAsyncActionFilter
    {
        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);

        private static IDictionary<string, SemaphoreSlim> LockMap = new Dictionary<string, SemaphoreSlim>();

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var method = actionDescriptor.MethodInfo;
            var items = GetConfiguration(method);

            var config = items.Item1;
            if (config != null)
            {
                var serviceProvider = context.HttpContext.RequestServices;
                var memoryCacheEntryOptions = items.Item2;
                var distributedCacheEntryOptions = items.Item3;

                if (memoryCacheEntryOptions != null || distributedCacheEntryOptions != null)
                {
                    var args = method.GetParameters().Select(p => context.ActionArguments[p.Name]).ToArray();

                    // The other way is to use Autofac PropertiesAutowired or OnActivating
                    var memoryCache = (IMemoryCache)serviceProvider.GetService(typeof(IMemoryCache));
                    var distributedCache = (IDistributedCache)serviceProvider.GetService(typeof(IDistributedCache));
                    var jsonSerializer = (IJsonSerializer)serviceProvider.GetService(typeof(IJsonSerializer));
                    var invocationContextSerializer = (IInvocationContextSerializer)serviceProvider.GetService(typeof(IInvocationContextSerializer));

                    var invocationKey = invocationContextSerializer.Serialize(method, config, args);

                    var delegateType = GetDelegateType(method);

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
                            isCached = memoryCache.TryGetValue(invocationKey, out returnValue);
                        }
                        else
                        {
                            // data
                            var bytes = distributedCache.Get(invocationKey);
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
                                    var entryValue = jsonSerializer.DeserializeObjectFromBson(bytes, entryValueType); // valueContainer

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
                        context.Result = (IActionResult)returnValue;
                    }
                    else
                    {
                        invocationLock.Wait(); // TODO: use finite timeout for safety

                        // Monitor.Enter(invocationLock);

                        var action = new Action<object>(result =>
                        {
                            if (memoryCacheEntryOptions != null)
                            {
                                memoryCache.Set(invocationKey, result, memoryCacheEntryOptions);
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
                                    bytes = jsonSerializer.SerializeObjectToBson(entryValue);
                                }

                                distributedCache.Set(invocationKey, bytes, distributedCacheEntryOptions);
                            }
                            invocationLock.Release();
                            // Monitor.Exit(invocationLock);
                        });

                        var resultContext = await next();
                        context.Result = resultContext.Result;
                        action(resultContext.Result);
                    }
                    return;
                }
            }

            {
                var resultContext = await next();
                context.Result = resultContext.Result;
            }
        }

        private (InvocationContextConfig InvocationContextConfig, MemoryCacheEntryOptions MemoryCacheEntryOptions, DistributedCacheEntryOptions DistributedCacheEntryOptions) GetConfiguration(MethodInfo mi)
        {
            var delegateType = GetDelegateType(mi);

            InvocationContextConfig invocationContextConfig = null;
            MemoryCacheEntryOptions memoryCacheEntryOptions = null;
            DistributedCacheEntryOptions distributedCacheEntryOptions = null;

            var attribute = (CacheAttribute)mi.GetCustomAttributes(typeof(CacheAttribute), false).LastOrDefault();

            if (attribute != null)
            {
                if (delegateType == MethodType.AsyncAction)
                {
                    // method call/invocation
                    throw new InvalidOperationException($"Can't cache method without result (\"{mi.Name}\")"); // ...with void result
                }

                invocationContextConfig = new InvocationContextConfig()
                {
                    ExcludeParameterIndexes = attribute.ExcludeParameterIndexes,
                    ExcludeParameterTypes = attribute.ExcludeParameterTypes,
                    InvocationContextSerializerType = attribute.InvocationContextSerializerType,
                    Tag = attribute.Tag
                };

                var cacheType = attribute.CacheType;

                if (cacheType == CacheType.InMemory)
                {
                    memoryCacheEntryOptions = new MemoryCacheEntryOptions();

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
                else if (cacheType == CacheType.Distributed)
                {
                    distributedCacheEntryOptions = new DistributedCacheEntryOptions();

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
            }

            return (invocationContextConfig, memoryCacheEntryOptions, distributedCacheEntryOptions);
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
}
