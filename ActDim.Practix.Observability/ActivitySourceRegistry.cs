#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ActDim.Practix.Observability
{
    /// <summary>
    /// Thread-safe registry for caching and reusing <see cref="ActivitySource"/> instances by name.
    /// </summary>
    internal static class ActivitySourceRegistry
    {
        private static readonly ConcurrentDictionary<string, ActivitySource> Sources = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets an existing <see cref="ActivitySource"/> or creates and caches a new long-lived instance.
        /// </summary>
        public static ActivitySource GetOrAdd(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("ActivitySource name cannot be null or whitespace.", nameof(name));
            }

            return Sources.GetOrAdd(name, static n => new ActivitySource(n));
        }
    }
}
