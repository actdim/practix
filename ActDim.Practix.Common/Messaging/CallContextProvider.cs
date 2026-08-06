using ActDim.Practix.Abstractions.Messaging;
using ActDim.Practix.Disposal;
using Ardalis.GuardClauses;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace ActDim.Practix.Messaging
{
    /// <summary>
    /// Ambient, provider-agnostic key/value context with scoped push/pop semantics.
    /// <para>
    /// The bag is stored as an immutable dictionary directly in an <see cref="AsyncLocal{T}"/>. Because
    /// every mutation assigns a brand-new dictionary to <see cref="AsyncLocal{T}.Value"/>, copy-on-write
    /// isolates each async flow: a <see cref="Set"/> in a child flow never leaks into its parent or siblings.
    /// </para>
    /// </summary>
    internal sealed class CallContextProvider : ICallContextProvider
    {
        private readonly AsyncLocal<ImmutableDictionary<string, object>> _current = new();

        private readonly CallContext _facade;

        private CallContextProvider()
        {
            _facade = new CallContext(this);
        }

        private static readonly Lazy<CallContextProvider> InternalInstance =
            new(() => new CallContextProvider());

        public static CallContextProvider Instance => InternalInstance.Value;

        public ICallContext Get()
        {
            return _facade;
        }

        internal ImmutableDictionary<string, object> Data
        {
            get
            {
                return _current.Value ?? [];
            }
        }

        internal IDisposable Set(string name, object value)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var previous = _current.Value ?? ImmutableDictionary<string, object>.Empty;
            var existed = previous.TryGetValue(name, out var oldValue);

            _current.Value = previous.SetItem(name, value);

            return new DisposableAction(() =>
            {
                var latest = _current.Value ?? [];
                if (existed)
                {
                    _current.Value = latest.SetItem(name, oldValue);
                }
                else
                {
                    _current.Value = latest.Remove(name);
                }
            });
        }
    }
}
