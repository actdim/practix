#nullable enable
using System;

namespace ActDim.Observability
{
    /// <summary>
    /// Telemetry tag names owned by <see cref="EventObservabilityBridge"/> itself.
    /// They live under the <see cref="Namespace"/> prefix so that application data,
    /// which is always emitted under its own domain names, cannot silently overwrite them.
    /// </summary>
    public static class ObservabilityTagNames
    {
        /// <summary>
        /// Prefix reserved for tags produced by the bridge. Application data never uses it.
        /// </summary>
        public const string Namespace = "log.";

        /// <summary>
        /// Number of tag writes that targeted an already occupied key while enriching the span.
        /// Written only when at least one collision occurred, so its presence always means telemetry was lost.
        /// </summary>
        public const string Collisions = "log.collisions";

        /// <summary>
        /// Determines whether the given tag name belongs to the reserved bridge namespace.
        /// </summary>
        public static bool IsReserved(string tagName)
        {
            return !string.IsNullOrEmpty(tagName) && tagName.StartsWith(Namespace, StringComparison.Ordinal);
        }
    }
}
