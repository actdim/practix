using ActDim.AppRegistry.Repo;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up AppRegistry repository services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AppRegistry repository services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddAppRegistryRepo(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<CommonRepo>();
            services.AddTransient<ProjectRepo>();
            services.AddTransient<RoleRepo>();
            services.AddTransient<UserRepo>();

            return services;
        }
    }
}
