using ActDim.Practix.Abstractions.Messaging;
using System;
using System.Collections.Generic;

namespace ActDim.Practix.Messaging
{
    /// <summary>
    /// Stateless facade over the ambient property bag owned by <see cref="CallContextProvider"/>.
    /// All state lives in the provider's <c>AsyncLocal</c>, so this instance carries nothing and is
    /// safe to share.
    /// </summary>
    internal sealed class CallContext : ICallContext
    {
        private readonly CallContextProvider _provider;

        internal CallContext(CallContextProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Pushes <paramref name="value"/> under <paramref name="name"/> for the current async flow.
        /// Disposing the returned handle restores the previous value (or removes the key if it was absent).
        /// </summary>
        public IDisposable Set(string name, object value)
        {
            return _provider.Set(name, value);
        }

        public IReadOnlyDictionary<string, object> Data => _provider.Data;
    }
}
