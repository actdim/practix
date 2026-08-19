using ActDim.BytePath;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering <see cref="IBlobManager"/> and configuring storage backends in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class BlobManagerServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the core blob manager engine (<see cref="IBlobManager"/>) and returns an <see cref="IBlobManagerBuilder"/> to configure storage and registry backends.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>A builder for fluently attaching data stores and registries.</returns>
        public static IBlobManagerBuilder AddBlobManager(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IBlobManager>(sp =>
            {
                var dataStores = sp.GetRequiredService<System.Collections.Generic.IEnumerable<IBlobDataStore>>();
                var registry = sp.GetRequiredService<IBlobRegistry>();
                return new BlobManager(dataStores, registry);
            });
            return new BlobManagerBuilder(services);
        }

        /// <summary>
        /// Registers the core blob manager engine (<see cref="IBlobManager"/>) and configures storage backends using the provided delegate.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <param name="configure">A configuration action on <see cref="IBlobManagerBuilder"/>.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddBlobManager(this IServiceCollection services, Action<IBlobManagerBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = services.AddBlobManager();
            configure?.Invoke(builder);
            return services;
        }

        /// <summary>
        /// Registers a custom implementation of <see cref="IBlobDataStore"/> on the blob manager builder.
        /// </summary>
        /// <typeparam name="TDataStore">The concrete data store type.</typeparam>
        /// <param name="builder">The blob manager builder.</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithDataStore<TDataStore>(this IBlobManagerBuilder builder)
            where TDataStore : class, IBlobDataStore
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IBlobDataStore, TDataStore>();
            return builder;
        }

        /// <summary>
        /// Registers a specific instance of <see cref="IBlobDataStore"/> on the blob manager builder.
        /// </summary>
        /// <param name="builder">The blob manager builder.</param>
        /// <param name="dataStore">The data store instance.</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithDataStore(this IBlobManagerBuilder builder, IBlobDataStore dataStore)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (dataStore == null)
            {
                throw new ArgumentNullException(nameof(dataStore));
            }

            builder.Services.AddSingleton<IBlobDataStore>(dataStore);
            return builder;
        }

        /// <summary>
        /// Registers a custom implementation of <see cref="IBlobRegistry"/> on the blob manager builder.
        /// </summary>
        /// <typeparam name="TRegistry">The concrete registry type.</typeparam>
        /// <param name="builder">The blob manager builder.</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithRegistry<TRegistry>(this IBlobManagerBuilder builder)
            where TRegistry : class, IBlobRegistry
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IBlobRegistry, TRegistry>();
            return builder;
        }

        /// <summary>
        /// Registers a specific instance of <see cref="IBlobRegistry"/> on the blob manager builder.
        /// </summary>
        /// <param name="builder">The blob manager builder.</param>
        /// <param name="registry">The registry instance.</param>
        /// <returns>The builder for fluent chaining.</returns>
        public static IBlobManagerBuilder WithRegistry(this IBlobManagerBuilder builder, IBlobRegistry registry)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            builder.Services.AddSingleton<IBlobRegistry>(registry);
            return builder;
        }
    }
}
