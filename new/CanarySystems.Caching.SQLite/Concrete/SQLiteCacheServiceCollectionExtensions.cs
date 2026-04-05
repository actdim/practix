using System;
using Microsoft.Extensions.Caching.Distributed;
using CanarySystems.Caching.SQLite;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up Microsoft SQL Server distributed cache services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class SQLiteCachingServicesExtensions
    {
        /// <summary>
        /// Adds Microsoft SQL Server distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{SQLiteCacheOptions}"/> to configure the provided <see cref="SQLiteCacheOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddDistributedSQLiteCache(this IServiceCollection services, Action<SQLiteCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            AddSQLiteCacheServices(services);
            services.Configure(setupAction);

            return services;
        }

        // to enable unit testing
        internal static void AddSQLiteCacheServices(IServiceCollection services)
        {
            services.Add(ServiceDescriptor.Singleton<IDistributedCache, SQLiteCache>());
        }
    }
}