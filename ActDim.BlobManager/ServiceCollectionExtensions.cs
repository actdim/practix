using ActDim.BlobManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;

namespace ActDim.BlobManager
{
    /// <summary>
    /// Extension methods for registering <see cref="IBlobManager"/>, <see cref="IBlobDataStore"/>, and <see cref="IBlobRegistry"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="FileSystemBlobDataStore"/> as <see cref="IBlobDataStore"/> with the specified base directory.
        /// </summary>
        public static IServiceCollection AddFileSystemBlobDataStore(
            this IServiceCollection services,
            string baseDirectory = null)
        {
            return services.AddFileSystemBlobDataStore(options =>
            {
                options.BaseDirectory = baseDirectory;
            });
        }

        /// <summary>
        /// Registers <see cref="FileSystemBlobDataStore"/> as <see cref="IBlobDataStore"/> using a configuration delegate.
        /// </summary>
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
            Directory.CreateDirectory(baseDir);

            services.TryAddSingleton<IBlobDataStore>(_ => new FileSystemBlobDataStore(baseDir));
            return services;
        }

        /// <summary>
        /// Registers <see cref="SQLiteBlobRegistry"/> as <see cref="IBlobRegistry"/> with specified database name and base directory.
        /// </summary>
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

        /// <summary>
        /// Registers <see cref="IBlobManager"/> and fallback file-system DataStore / SQLite Registry implementations if not already registered.
        /// </summary>
        public static IServiceCollection AddBlobManager(
            this IServiceCollection services,
            string baseDirectory = null,
            string databaseName = "registry.db")
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddFileSystemBlobDataStore(baseDirectory);
            services.AddSQLiteBlobRegistry(databaseName, baseDirectory);
            services.TryAddSingleton<IBlobManager, BlobManager>();

            return services;
        }
    }
}
