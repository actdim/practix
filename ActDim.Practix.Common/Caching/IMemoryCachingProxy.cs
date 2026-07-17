
using Microsoft.Extensions.Caching.Memory;
using System;

namespace ActDim.Practix.Caching
{
    /// <summary>
    /// Memoizing proxy over an <see cref="IMemoryCache"/>: wraps a value-producing function into a
    /// delegate that serves cached values and only invokes the underlying function on a miss.
    /// </summary>
    public interface IMemoryCachingProxy
    {
        /// <summary>
        /// Wraps <paramref name="func"/> into a memoizing delegate: on each call it first tries to
        /// read the value from the cache, and only invokes <paramref name="func"/> on a miss,
        /// storing the produced value under the same key. When <typeparamref name="T"/> is an
        /// awaitable (<see cref="System.Threading.Tasks.Task{TResult}"/> /
        /// <see cref="System.Threading.Tasks.ValueTask{TResult}"/>) the lookup, the invocation and
        /// the store are all performed asynchronously.
        /// </summary>
        Func<string, T> Get<T>(Func<string, T> func, MemoryCacheEntryOptions options);
    }
}
