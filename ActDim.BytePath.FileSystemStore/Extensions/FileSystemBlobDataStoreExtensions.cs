using ActDim.BytePath;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring <see cref="FileSystemBlobDataStore"/> in an <see cref="IServiceCollection"/> and on <see cref="IBlobManagerBuilder"/>.
    /// </summary>
    public static class FileSystemBlobDataStoreExtensions
    {
        /// <summary>
        /// Registers <see cref="FileSystemBlobDataStore"/> as a data store on the <see cref="IBlobManagerBuilder"/> with the specified base directory and key prefix.
        /// </summary>
        /// <param name="builder">The blob manager builder.</param>
        /// <param name="baseDirectory">The root directory for storing blobs (defaults to './blobs').</param>
        /// <param name="keyPrefix">The key prefix handled by this store (e.g. <c>"fs:"</c>). Defaults to empty string (catch-all).</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithFileSystemDataStore(
            this IBlobManagerBuilder builder,
            string baseDirectory = null,
            string keyPrefix = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddFileSystemBlobDataStore(baseDirectory, keyPrefix);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="FileSystemBlobDataStore"/> as a data store on the <see cref="IBlobManagerBuilder"/> using a configuration delegate.
        /// </summary>
        /// <param name="builder">The blob manager builder.</param>
        /// <param name="configure">An action to configure <see cref="FileSystemBlobDataStoreOptions"/>.</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithFileSystemDataStore(
            this IBlobManagerBuilder builder,
            Action<FileSystemBlobDataStoreOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddFileSystemBlobDataStore(configure);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="FileSystemBlobDataStore"/> as <see cref="IBlobDataStore"/> with the specified base directory and key prefix.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="baseDirectory">The root directory for storing blobs (defaults to './blobs').</param>
        /// <param name="keyPrefix">The key prefix handled by this store (e.g. <c>"fs:"</c>). Defaults to empty string (catch-all).</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddFileSystemBlobDataStore(
            this IServiceCollection services,
            string baseDirectory = null,
            string keyPrefix = null)
        {
            return services.AddFileSystemBlobDataStore(options =>
            {
                options.BaseDirectory = baseDirectory;
                if (keyPrefix != null)
                {
                    options.KeyPrefix = keyPrefix;
                }
            });
        }

        /// <summary>
        /// Registers <see cref="FileSystemBlobDataStore"/> as <see cref="IBlobDataStore"/> using a configuration delegate.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configure">An action to configure <see cref="FileSystemBlobDataStoreOptions"/>.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddFileSystemBlobDataStore(
            this IServiceCollection services,
            Action<FileSystemBlobDataStoreOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new FileSystemBlobDataStoreOptions();
            configure?.Invoke(options);

            var baseDir = options.BaseDirectory;
            baseDir ??= Path.Combine(Directory.GetCurrentDirectory(), "blobs");
            options.BaseDirectory = baseDir;
            Directory.CreateDirectory(baseDir);

            services.AddSingleton<IBlobDataStore>(_ => new FileSystemBlobDataStore(options));
            return services;
        }
    }
}
