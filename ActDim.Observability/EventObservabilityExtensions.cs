#nullable enable
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace ActDim.Observability
{
    /// <summary>
    /// Extension methods for registering <see cref="EventObservabilityBridge"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class EventObservabilityExtensions
    {
        /// <summary>
        /// Adds event observability logging decoration and OpenTelemetry Activity trace enrichment to the service collection.
        /// </summary>
        public static IServiceCollection AddEventObservability(
            this IServiceCollection services,
            Action<ILoggingBuilder>? configureLogging = null,
            Action<EventObservabilityOptions>? configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            services.TryAddSingleton<IAmbientContextProvider>(sp => AmbientContextProvider.Instance);
            services.TryAddSingleton<IObservabilityContext>(sp => new ObservabilityContext(sp.GetRequiredService<IAmbientContextProvider>()));

            services.AddLogging(builder =>
            {
                configureLogging?.Invoke(builder);
            });

            // Decorate all ILoggerProvider descriptors in ServiceCollection to support selective provider suppression
            WrapRegisteredLoggerProviders(services);

            // Decorate ILoggerFactory
            services.DecorateLoggerFactory();

            return services;
        }

        private static void WrapRegisteredLoggerProviders(IServiceCollection services)
        {
            var providerDescriptors = services.Where(d => d.ServiceType == typeof(ILoggerProvider)).ToList();
            foreach (var descriptor in providerDescriptors)
            {
                services.Remove(descriptor);

                services.Add(new ServiceDescriptor(
                    typeof(ILoggerProvider),
                    sp =>
                    {
                        ILoggerProvider innerProvider;
                        if (descriptor.ImplementationInstance != null)
                        {
                            innerProvider = (ILoggerProvider)descriptor.ImplementationInstance;
                        }
                        else if (descriptor.ImplementationFactory != null)
                        {
                            innerProvider = (ILoggerProvider)descriptor.ImplementationFactory(sp);
                        }
                        else
                        {
                            innerProvider = (ILoggerProvider)ActivatorUtilities.GetServiceOrCreateInstance(sp, descriptor.ImplementationType!);
                        }

                        var options = sp.GetRequiredService<IOptions<EventObservabilityOptions>>().Value;
                        var ambientContextProvider = sp.GetService<IAmbientContextProvider>();
                        var scopeProvider = sp.GetService<IExternalScopeProvider>();
                        var alias = EventObservabilityLoggerFactory.ResolveProviderAlias(innerProvider, options);

                        return new EventObservabilityProviderDecorator(innerProvider, alias, ambientContextProvider, scopeProvider);
                    },
                    descriptor.Lifetime));
            }
        }

        private static void DecorateLoggerFactory(this IServiceCollection services)
        {
            var factoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));

            if (factoryDescriptor != null)
            {
                services.Remove(factoryDescriptor);

                services.Add(new ServiceDescriptor(
                    typeof(ILoggerFactory),
                    sp =>
                    {
                        ILoggerFactory innerFactory;
                        if (factoryDescriptor.ImplementationInstance != null)
                        {
                            innerFactory = (ILoggerFactory)factoryDescriptor.ImplementationInstance;
                        }
                        else if (factoryDescriptor.ImplementationFactory != null)
                        {
                            innerFactory = (ILoggerFactory)factoryDescriptor.ImplementationFactory(sp);
                        }
                        else
                        {
                            innerFactory = (ILoggerFactory)ActivatorUtilities.GetServiceOrCreateInstance(sp, factoryDescriptor.ImplementationType!);
                        }

                        var options = sp.GetRequiredService<IOptions<EventObservabilityOptions>>().Value;
                        var ambientContextProvider = sp.GetService<IAmbientContextProvider>();
                        var scopeProvider = sp.GetService<IExternalScopeProvider>();

                        return new EventObservabilityLoggerFactory(innerFactory, ambientContextProvider, scopeProvider, options);
                    },
                    factoryDescriptor.Lifetime));
            }
            else
            {
                services.AddSingleton<ILoggerFactory>(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<EventObservabilityOptions>>().Value;
                    var ambientContextProvider = sp.GetService<IAmbientContextProvider>();
                    var scopeProvider = sp.GetService<IExternalScopeProvider>();

                    var innerLoggerFactory = LoggerFactory.Create(_ => { });
                    return new EventObservabilityLoggerFactory(innerLoggerFactory, ambientContextProvider, scopeProvider, options);
                });
            }
        }
    }
}
