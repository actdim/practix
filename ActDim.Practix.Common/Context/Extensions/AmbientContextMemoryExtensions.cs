using ActDim.Practix.Abstractions.Context;
using Microsoft.IO;
using System;

namespace ActDim.Practix.Abstractions.Memory
{
    /// <summary>
    /// Extension methods providing typed access and scoped overrides for <see cref="RecyclableMemoryStreamManager"/> on <see cref="IAmbientContext"/>.
    /// </summary>
    public static class AmbientContextMemoryExtensions
    {
        /// <summary>
        /// Gets the scoped <see cref="RecyclableMemoryStreamManager"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static RecyclableMemoryStreamManager? GetMemoryManager(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            if (context.Properties.TryGetValue(AmbientKeys.MemoryManager, out var val) && val is RecyclableMemoryStreamManager mm)
            {
                return mm;
            }

            return null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="RecyclableMemoryStreamManager"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithMemoryManager(this IAmbientContext context, RecyclableMemoryStreamManager memoryManager)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(memoryManager, nameof(memoryManager));
            return context.PushProperty(AmbientKeys.MemoryManager, memoryManager);
        }
    }
}

