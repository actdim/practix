using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Disposal;
using Ardalis.GuardClauses;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace ActDim.Practix.Context
{
    /// <inheritdoc />
    public sealed class CallContextProvider : ICallContextProvider
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

        /// <inheritdoc />
        public ICallContext Get()
        {
            return _facade;
        }

        internal ImmutableDictionary<string, object> Data
        {
            get
            {
                return _current.Value ?? ImmutableDictionary<string, object>.Empty;
            }
        }

        internal IDisposable Push(string name, object value)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var previous = _current.Value ?? ImmutableDictionary<string, object>.Empty;
            var existed = previous.TryGetValue(name, out var oldValue);

            _current.Value = previous.SetItem(name, value);

            return new DisposableAction(() =>
            {
                var latest = _current.Value ?? ImmutableDictionary<string, object>.Empty;
                if (existed)
                {
                    _current.Value = latest.SetItem(name, oldValue!);
                }
                else
                {
                    _current.Value = latest.Remove(name);
                }
            });
        }
    }
}
