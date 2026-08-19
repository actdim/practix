using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up JSON serialization services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <c>ActDim.Practix.Json</c> serialization services (<see cref="IJsonSerializer"/> backed by <see cref="CoreJsonSerializer"/>) to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddPractixJson(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IJsonSerializer, CoreJsonSerializer>();
            return services;
        }

        /// <summary>
        /// Adds <c>ActDim.Practix.Json</c> serialization services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddJsonSerializer(this IServiceCollection services)
        {
            return services.AddPractixJson();
        }
    }
}
