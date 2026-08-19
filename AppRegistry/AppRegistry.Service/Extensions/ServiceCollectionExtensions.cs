using ActDim.AppRegistry.Service;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up AppRegistry service layer dependencies in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AppRegistry services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddAppRegistryService(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<IProjectProvider, ProjectProvider>();
            services.AddTransient<IAppRegistryService, AppRegistryService>();

            return services;
        }
    }
}
