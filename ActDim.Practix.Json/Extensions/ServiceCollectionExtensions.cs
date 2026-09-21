using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Serialization;
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
        /// Adds core JSON serialization services backed by <see cref="CoreJsonSerializer"/> to the specified <see cref="IServiceCollection"/>,
        /// registering a shared singleton for <see cref="IJsonSerializer"/>, <see cref="IStringSerializer"/>,
        /// <see cref="IBinarySerializer"/>, and <see cref="IStreamSerializer"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCoreJsonSerializer(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            services.TryAddSingleton<CoreJsonSerializer>(static _ => new CoreJsonSerializer());
            services.TryAddSingleton<IJsonSerializer>(static sp => sp.GetRequiredService<CoreJsonSerializer>());
            services.TryAddSingleton<IStringSerializer>(static sp => sp.GetRequiredService<CoreJsonSerializer>());
            services.TryAddSingleton<IBinarySerializer>(static sp => sp.GetRequiredService<CoreJsonSerializer>());
            services.TryAddSingleton<IStreamSerializer>(static sp => sp.GetRequiredService<CoreJsonSerializer>());

            return services;
        }
    }
}
