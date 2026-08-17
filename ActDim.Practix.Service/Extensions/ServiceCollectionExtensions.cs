using ActDim.Practix.Common.Extensions;
using ActDim.Practix.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ActDim.Practix.Service.Extensions
{
    /// <summary>
    /// Extension methods for setting up core Practix application service dependencies in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds core Practix framework service dependencies to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCoreService(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddAmbientContext();
            services.AddCompressionManager();
            services.AddMemoryCachingProxy();
            services.AddDistributedCachingProxy();
            services.AddPractixJson();

            return services;
        }
    }
}
