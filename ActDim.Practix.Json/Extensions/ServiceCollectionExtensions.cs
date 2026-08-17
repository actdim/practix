using ActDim.Practix.Abstractions.Json;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ActDim.Practix.Json.Extensions
{
    /// <summary>
    /// Extension methods for setting up JSON serialization services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <c>ActDim.Practix.Json</c> serialization services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddPractixJson(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IJsonSerializer, CoreJsonSerializer>();

            return services;
        }
    }
}
