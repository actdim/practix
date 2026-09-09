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
            ArgumentNullException.ThrowIfNull(services);
            services.AddTransient<CommonRepo>();
            services.AddTransient<IProjectRepo, ProjectRepo>();
            services.AddTransient<IRoleRepo, RoleRepo>();
            services.AddTransient<IUserRepo, UserRepo>();
            return services;
        }
    }
}
