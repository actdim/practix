using ActDim.Practix.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System;

namespace ActDim.Practix.Abstractions.Logging
{
    /// <summary>
    /// Extension methods providing typed access and scoped overrides for <see cref="ILoggerFactory"/> on <see cref="IAmbientContext"/>.
    /// </summary>
    public static class AmbientContextLoggingExtensions
    {
        /// <summary>
        /// Gets the scoped <see cref="ILoggerFactory"/> from the ambient context properties, or <c>null</c> if not set.
        /// </summary>
        public static ILoggerFactory? GetLoggerFactory(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.LoggerFactory, out var val) && val is ILoggerFactory lf ? lf : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="ILoggerFactory"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithLoggerFactory(this IAmbientContext context, ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
            return context.PushProperty(AmbientKeys.LoggerFactory, loggerFactory);
        }
    }
}

