using System;

namespace ActDim.Observability
{
    /// <summary>
    /// Telemetry tag names owned by <see cref="EventObservabilityBridge"/> and standard semantic conventions.
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
        /// Standard OpenTelemetry Source Code Semantic Conventions.
        /// Adhering to these standard attribute keys enables APM tools, log aggregators,
        /// and trace visualizers (Jaeger, Grafana Tempo, Datadog, Dynatrace, .NET Aspire) to natively
        /// recognize, index, filter, and navigate directly to source code locations.
        /// </summary>
        public static class Code
        {
            /// <summary>
            /// The method or function name (OpenTelemetry semantic convention: <c>code.function</c>).
            /// </summary>
            public const string Function = "code.function";

            /// <summary>
            /// The source code file path (OpenTelemetry semantic convention: <c>code.filepath</c>).
            /// </summary>
            public const string FilePath = "code.filepath";

            /// <summary>
            /// The line number in code (OpenTelemetry semantic convention: <c>code.lineno</c>).
            /// </summary>
            public const string LineNumber = "code.lineno";

            /// <summary>
            /// The source code file name (OpenTelemetry semantic convention: <c>code.filename</c>).
            /// </summary>
            public const string FileName = "code.filename";
        }

        /// <summary>
        /// Determines whether the given tag name belongs to the reserved bridge namespace.
        /// </summary>
        public static bool IsReserved(string tagName)
        {
            return !string.IsNullOrEmpty(tagName) && tagName.StartsWith(Namespace, StringComparison.Ordinal);
        }
    }
}
