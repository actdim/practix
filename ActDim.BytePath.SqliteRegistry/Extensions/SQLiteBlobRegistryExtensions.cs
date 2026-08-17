using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;

namespace ActDim.BytePath
{
    /// <summary>
    /// Extension methods for configuring <see cref="SQLiteBlobRegistry"/> in an <see cref="IServiceCollection"/> and on <see cref="IBlobManagerBuilder"/>.
    /// </summary>
    public static class SQLiteBlobRegistryExtensions
    {
        /// <summary>
        /// Registers <see cref="SQLiteBlobRegistry"/> as the registry on the <see cref="IBlobManagerBuilder"/> with specified database name and base directory.
        /// </summary>
        /// <param name="builder">The blob manager builder.</param>
        /// <param name="databaseName">The SQLite database file name (defaults to 'registry.db').</param>
        /// <param name="baseDirectory">The root directory where the database file will be stored (defaults to './blobs').</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithSQLiteRegistry(
            this IBlobManagerBuilder builder,
            string databaseName = "registry.db",
            string baseDirectory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSQLiteBlobRegistry(databaseName, baseDirectory);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="SQLiteBlobRegistry"/> as the registry on the <see cref="IBlobManagerBuilder"/> using a configuration delegate.
        /// </summary>
        /// <param name="builder">The blob manager builder.</param>
        /// <param name="configure">An action to configure <see cref="SQLiteBlobRegistryOptions"/>.</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithSQLiteRegistry(
            this IBlobManagerBuilder builder,
            Action<SQLiteBlobRegistryOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSQLiteBlobRegistry(configure);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="SQLiteBlobRegistry"/> as <see cref="IBlobRegistry"/> with specified database name and base directory.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="databaseName">The SQLite database file name (defaults to 'registry.db').</param>
        /// <param name="baseDirectory">The root directory where the database file will be stored (defaults to './blobs').</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddSQLiteBlobRegistry(
            this IServiceCollection services,
            string databaseName = "registry.db",
            string baseDirectory = null)
        {
            return services.AddSQLiteBlobRegistry(options =>
            {
                options.DatabaseName = databaseName;
                options.BaseDirectory = baseDirectory;
            });
        }

        /// <summary>
        /// Registers <see cref="SQLiteBlobRegistry"/> as <see cref="IBlobRegistry"/> using a configuration delegate.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configure">An action to configure <see cref="SQLiteBlobRegistryOptions"/>.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddSQLiteBlobRegistry(
            this IServiceCollection services,
            Action<SQLiteBlobRegistryOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new SQLiteBlobRegistryOptions();
            configure?.Invoke(options);

            var baseDir = options.BaseDirectory;
            baseDir ??= Path.Combine(Directory.GetCurrentDirectory(), "blobs");
            Directory.CreateDirectory(baseDir);

            var dbName = string.IsNullOrWhiteSpace(options.DatabaseName) ? "registry.db" : options.DatabaseName;
            var dbPath = Path.IsPathRooted(dbName) ? dbName : Path.Combine(baseDir, dbName);

            services.TryAddSingleton<IBlobRegistry>(_ => new SQLiteBlobRegistry(dbPath));
            return services;
        }
    }
}
