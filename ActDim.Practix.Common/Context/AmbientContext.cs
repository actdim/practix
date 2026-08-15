#nullable enable
using ActDim.Practix.Abstractions.Context;
using System;
using System.Collections.Generic;

namespace ActDim.Practix.Context
{
    /// <inheritdoc />
    public sealed class AmbientContext : IAmbientContext
    {
        private readonly AmbientContextProvider _provider;

        internal AmbientContext(AmbientContextProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc />
        public IDisposable PushProperty(string name, object value)
        {
            return _provider.PushProperty(name, value);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Properties => _provider.Properties;

        // ══ Static Convenience API (zero-DI ceremony) ═════════════════════════

        /// <summary>
        /// Gets the current ambient context instance for the calling async flow.
        /// </summary>
        public static IAmbientContext Current => AmbientContextProvider.Instance.Get();

        /// <summary>
        /// Gets the current ambient context properties for the calling async flow.
        /// </summary>
        public static IReadOnlyDictionary<string, object> CurrentProperties => Current.Properties;

        /// <summary>
        /// Pushes a property into the ambient context for the current async flow.
        /// </summary>
        public static IDisposable Push(string name, object value)
        {
            return Current.PushProperty(name, value);
        }
    }
}
