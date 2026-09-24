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
        private static readonly AsyncLocal<ImmutableDictionary<string, object>> _current = new();
        private static readonly AmbientContext _instance = new();

        private AmbientContext()
        {
        }

        /// <inheritdoc />
        public IDisposable PushProperty(string name, object value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

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

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Properties => _current.Value ?? ImmutableDictionary<string, object>.Empty;

        // == Static Convenience API (zero-DI ceremony) =========================

        /// <summary>
        /// Gets the current ambient context instance for the calling async flow.
        /// </summary>
        public static IAmbientContext Current => _instance;

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
