using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Caching;
using ActDim.Practix.Compression;
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ActDim.Practix.Common.Extensions
{
    /// <summary>
    /// Extension methods for setting up core <c>ActDim.Practix.Common</c> services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IAmbientContextProvider"/> in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddAmbientContext(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IAmbientContextProvider>(AmbientContextProvider.Instance);
            return services;
        }

        /// <summary>
        /// Registers <see cref="ICompressionManager"/> in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddCompressionManager(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<ICompressionManager, CompressionManager>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="IMemoryCachingProxy"/> in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddMemoryCachingProxy(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<IMemoryCachingProxy, MemoryCachingProxy>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="IDistributedCachingProxy"/> in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddDistributedCachingProxy(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<IDistributedCachingProxy, DistributedCachingProxy>();
            return services;
        }
    }
}
