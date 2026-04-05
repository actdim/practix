using System;

namespace CanarySystems.Caching
{
    /// <summary>
    /// Memoize
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class CacheAttribute : Attribute
    {
        /// <summary>
        /// IMemory/Distributed. Allows NULL to provide the way to extend fluent method configuration.
        /// </summary>
        public CacheType? CacheType { get; set; }

        /// <summary>
        /// Iterface type to proxify
        /// </summary>
        public Type InterfaceType { get; set; }

        /// <summary>
        /// User data passed to invocation context serializer. Can be used as custom method ID in serializer.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Custom IInvocationContextSerializer implemetation type
        /// </summary>
        public Type InvocationContextSerializerType { get; set; }

        /// <summary>
        /// Allows to exclude some parameter from key serialization by index. 
        /// </summary>
        public int[] ExcludeParameterIndexes { get; set; }

        /// <summary>
        /// Allows to exclude some parameter from key serialization by type. 
        /// </summary>
        public Type[] ExcludeParameterTypes { get; set; }

        /// <summary>
        /// Absolute expiration date for the cache entry.
        /// </summary>
        public string AbsoluteExpiration { get; set; }

        /// <summary>
        /// Absolute expiration time, relative to now
        /// </summary>
        public string AbsoluteExpirationRelativeToNow { get; set; }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before
        /// it will be removed. This will not extend the entry lifetime beyond the absolute
        /// expiration (if set).
        /// </summary>
        public string SlidingExpiration { get; set; }

        public CacheAttribute(CacheType cacheType)
        {
            CacheType = cacheType;
        }

        public CacheAttribute(Type interfaceType)
        {
            InterfaceType = interfaceType;
        }

        public CacheAttribute()
        {

        }
    }
}
