using ActDim.Practix.Abstractions.Context;
using System;

namespace ActDim.Practix.Abstractions.Compression
{
    /// <summary>
    /// Extension methods providing typed access and scoped overrides for <see cref="ICompressionManager"/> on <see cref="IAmbientContext"/>.
    /// </summary>
    public static class AmbientContextCompressionExtensions
    {
        /// <summary>
        /// Gets the scoped <see cref="ICompressionManager"/> from the ambient context properties, or <c>null</c> if not set.
        /// </summary>
        public static ICompressionManager? GetCompressionManager(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.CompressionManager, out var val) && val is ICompressionManager cm ? cm : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="ICompressionManager"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithCompressionManager(this IAmbientContext context, ICompressionManager compressionManager)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(compressionManager, nameof(compressionManager));
            return context.PushProperty(AmbientKeys.CompressionManager, compressionManager);
        }
    }
}

