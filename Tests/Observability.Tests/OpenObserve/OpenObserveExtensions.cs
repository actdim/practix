using ActDim.Observability.Tests.OpenObserve;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.Logging
{
    public static class OpenObserveExtensions
    {
        public static ILoggingBuilder AddOpenObserve(this ILoggingBuilder builder, Action<OpenObserveOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            var options = new OpenObserveOptions();
            configure?.Invoke(options);

            builder.Services.TryAddSingleton(options);
            builder.Services.TryAddSingleton<OpenObserveClient>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OpenObserveLoggerProvider>());

            return builder;
        }
    }
}
