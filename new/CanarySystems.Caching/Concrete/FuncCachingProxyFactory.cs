using System;
using System.Linq.Expressions;

namespace CanarySystems.Caching
{
    /// <summary>
    /// FuncMemoizationProxy..., FuncCachingInterceptor... Provider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FuncCachingProxyFactory<TDelegate> : IFuncCachingProxyFactory<TDelegate>
    {
        //public T Create(T arg)
        //{
        //	if (!typeof(T).IsInterface)
        //	{
        //		// type parameter
        //		// throw new ArgumentException($"An interface excpected as generic argument"); // {nameof(T)}
        //		return new ProxyGenerator().CreateClassProxyWithTarget(arg, new CachingInterceptor<T>());
        //	}
        //	return new ProxyGenerator().CreateInterfaceProxyWithTarget(arg, new CachingInterceptor<T>());
        //}

        public TDelegate Create(Expression<TDelegate> arg)
        {
            throw new NotImplementedException();
        }
    }
}
