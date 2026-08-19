using ActDim.Observability.Tests.VictoriaLogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.Logging
{
    public static class VictoriaLogsExtensions
    {
        public static ILoggingBuilder AddVictoriaLogs(this ILoggingBuilder builder, Action<VictoriaLogsOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            var options = new VictoriaLogsOptions();
            configure?.Invoke(options);

            builder.Services.TryAddSingleton(options);
            builder.Services.TryAddSingleton<VictoriaLogsClient>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, VictoriaLogsLoggerProvider>());

            return builder;
        }
    }
}
