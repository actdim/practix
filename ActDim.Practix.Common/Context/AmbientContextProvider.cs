#nullable enable
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Disposal;
using Ardalis.GuardClauses;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace ActDim.Practix.Context
{
    /// <inheritdoc />
    public sealed class AmbientContextProvider : IAmbientContextProvider
    {
        private readonly AsyncLocal<ImmutableDictionary<string, object>> _current = new();
        private readonly AmbientContext _facade;

        private AmbientContextProvider()
        {
            _facade = new AmbientContext(this);
        }

        private static readonly Lazy<AmbientContextProvider> InternalInstance = new(() => new AmbientContextProvider());

        /// <summary>
        /// Gets the singleton instance of <see cref="AmbientContextProvider"/>.
        /// </summary>
        public static AmbientContextProvider Instance => InternalInstance.Value;

        /// <inheritdoc />
        public IAmbientContext Get()
        {
            return _facade;
        }

        internal ImmutableDictionary<string, object> Properties
        {
            get
            {
                return _current.Value ?? ImmutableDictionary<string, object>.Empty;
            }
        }

        internal IDisposable PushProperty(string name, object value)
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
