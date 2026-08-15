#nullable enable

namespace ActDim.Practix.Observability
{
    /// <summary>
    /// Well-known ambient property names owned by <see cref="IObservabilityContext"/>.
    /// </summary>
    /// <remarks>
    /// Two kinds of keys live here. Data keys (<see cref="Status"/>, <see cref="Progress"/>, <see cref="Icon"/>,
    /// or custom keys pushed via <see cref="IObservabilityContext.Push"/>) describe the operation and are exported as
    /// <see cref="System.Diagnostics.Activity"/> tags (OpenTelemetry span attributes). Control keys, prefixed with
    /// <c>__Practix_</c>, configure the telemetry pipeline for the duration of a scope and are never exported.
    /// </remarks>
    public static class ObservabilityContextPropertyNames
    {
        /// <summary>
        /// Control key tracking the set of property names explicitly marked for export as <see cref="System.Diagnostics.Activity"/> tags.
        /// </summary>
        public const string ExportedKeys = "__Practix_ExportedKeys";

        /// <summary>
        /// Control key deciding whether <see cref="Microsoft.Extensions.Logging.IExternalScopeProvider"/> scopes are written into
        /// <see cref="System.Diagnostics.Activity"/> tags (default: false).
        /// </summary>
        public const string IncludeExternalScopes = "__Practix_IncludeExternalScopes";

        /// <summary>
        /// Control key deciding whether console log provider outputs are suppressed (default: false).
        /// </summary>
        public const string SuppressConsole = "__Practix_SuppressConsole";

        /// <summary>
        /// Control key holding the set of suppressed logger provider names or aliases.
        /// </summary>
        public const string SuppressedProviders = "__Practix_SuppressedProviders";

        /// <summary>
        /// Control key holding the ActivitySource name used when an <see cref="System.Diagnostics.Activity"/> is started automatically on BeginScope.
        /// </summary>
        public const string ActivitySourceName = "__Practix_ActivitySourceName";

        /// <summary>
        /// Data key holding the current operation status text (e.g. "Downloading").
        /// </summary>
        public const string Status = "status";

        /// <summary>
        /// Data key holding the operation progress percentage (0..100).
        /// </summary>
        public const string Progress = "progress";

        /// <summary>
        /// Data key holding the visual status icon or emoji (e.g. "🚀", "⚡").
        /// </summary>
        public const string Icon = "icon";

        /// <summary>
        /// Determines whether the given ambient property is an internal control flag that must never be exported.
        /// </summary>
        public static bool IsControlKey(string name)
        {
            return name != null && name.StartsWith("__Practix_", System.StringComparison.Ordinal);
        }
    }
}
