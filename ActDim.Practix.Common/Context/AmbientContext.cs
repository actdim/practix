using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Disposal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ActDim.Practix.Context
{
    /// <inheritdoc />
    public sealed class AmbientContext : IAmbientContext
    {
        private static readonly AsyncLocal<ImmutableDictionary<string, object>> _storage = new();

        private AmbientContext()
        {
        }

        /// <summary>
        /// Gets the current ambient context instance for the calling async flow.
        /// </summary>
        public static IAmbientContext Current { get; } = new AmbientContext();

        /// <inheritdoc />
        public IDisposable PushProperty(string name, object value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

            var previous = _storage.Value ?? ImmutableDictionary<string, object>.Empty;
            var existed = previous.TryGetValue(name, out var oldValue);

            _storage.Value = previous.SetItem(name, value);

            return new DisposableAction(() =>
            {
                var latest = _storage.Value ?? ImmutableDictionary<string, object>.Empty;
                if (existed)
                {
                    _storage.Value = latest.SetItem(name, oldValue!);
                }
                else
                {
                    _storage.Value = latest.Remove(name);
                }
            });
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Properties => _storage.Value ?? ImmutableDictionary<string, object>.Empty;
    }
}
