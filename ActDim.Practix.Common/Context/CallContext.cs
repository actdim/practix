#nullable enable
using ActDim.Practix.Abstractions.Context;
using System;
using System.Collections.Generic;

namespace ActDim.Practix.Context
{
    /// <inheritdoc />
    public sealed class CallContext : ICallContext
    {
        private readonly CallContextProvider _provider;

        internal CallContext(CallContextProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc />
        public IDisposable Push(string name, object value)
        {
            return _provider.Push(name, value);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Data => _provider.Data;

        // ══ Static Convenience API (zero-DI ceremony) ═════════════════════════

        /// <summary>
        /// Gets the current ambient context properties for the calling async flow.
        /// </summary>
        public static IReadOnlyDictionary<string, object> CurrentData => CallContextProvider.Instance.Get().Data;

        /// <summary>
        /// Pushes a property into the ambient call context for the current async flow.
        /// </summary>
        public static IDisposable PushProperty(string name, object value) => CallContextProvider.Instance.Get().Push(name, value);
    }
}
