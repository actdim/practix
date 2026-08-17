#nullable enable
using System;
using System.Collections.Generic;

namespace ActDim.Observability
{
    /// <summary>
    /// Accumulates telemetry tags produced by a single observability write, applies the configured
    /// <see cref="TagCollisionBehavior"/> when a key is written more than once, and counts every collision
    /// so that tag loss never stays invisible.
    /// </summary>
    internal sealed class TelemetryTagCollector
    {
        private readonly Dictionary<string, object?> _tags = [];
        private readonly TagCollisionBehavior _behavior;

        public TelemetryTagCollector(TagCollisionBehavior behavior)
        {
            _behavior = behavior;
        }

        /// <summary>
        /// Gets the number of writes that targeted an already occupied key.
        /// </summary>
        public int CollisionCount { get; private set; }

        /// <summary>
        /// Gets the collected tags.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Tags => _tags;

        /// <summary>
        /// Writes a single tag, resolving a collision according to <see cref="TagCollisionBehavior"/>.
        /// </summary>
        public void Write(string key, object? value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_tags.ContainsKey(key))
            {
                _tags[key] = value;
                return;
            }

            CollisionCount++;

            switch (_behavior)
            {
                case TagCollisionBehavior.Overwrite:
                    _tags[key] = value;
                    break;

                case TagCollisionBehavior.Throw:
                    throw new InvalidOperationException(
                        $"Telemetry tag '{key}' is written more than once within a single log call. " +
                        $"Existing value: '{_tags[key]}', rejected value: '{value}'.");

                default:
                    break;
            }
        }

        /// <summary>
        /// Writes every entry of the given sequence.
        /// </summary>
        public void WriteRange(IEnumerable<KeyValuePair<string, object>> items)
        {
            foreach (var item in items)
            {
                Write(item.Key, item.Value);
            }
        }
    }
}
