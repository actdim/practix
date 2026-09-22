using ActDim.Practix.Abstractions.Context;
using System;

namespace ActDim.Practix.Abstractions.Storage
{
    /// <summary>
    /// Extension methods providing typed access and scoped overrides for <see cref="IBlobManager"/> on <see cref="IAmbientContext"/>.
    /// </summary>
    public static class AmbientContextBlobExtensions
    {
        /// <summary>
        /// Gets the scoped <see cref="IBlobManager"/> from the ambient context properties, or <c>null</c> if not set.
        /// </summary>
        public static IBlobManager? GetBlobManager(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.BlobManager, out var val) && val is IBlobManager bm ? bm : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="IBlobManager"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithBlobManager(this IAmbientContext context, IBlobManager blobManager)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(blobManager, nameof(blobManager));
            return context.PushProperty(AmbientKeys.BlobManager, blobManager);
        }
    }
}

