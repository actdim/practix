namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Well-known property names used within <see cref="ICallContext"/>.
    /// </summary>
    public static class CallContextPropertyNames
    {
        /// <summary>
        /// Context key controlling whether IExternalScopeProvider scopes are written into telemetry tags (default: true).
        /// </summary>
        public const string IncludeExternalScopes = "__Practix_IncludeExternalScopes";

        /// <summary>
        /// Context key controlling whether ambient ICallContext data is written into telemetry tags (default: true).
        /// </summary>
        public const string IncludeCallContext = "__Practix_IncludeCallContext";

        /// <summary>
        /// Context key controlling whether console log provider outputs are suppressed (default: false).
        /// </summary>
        public const string SuppressConsole = "__Practix_SuppressConsole";

        /// <summary>
        /// Context key containing a HashSet or CSV string of suppressed provider names/aliases.
        /// </summary>
        public const string SuppressedProviders = "__Practix_SuppressedProviders";

        /// <summary>
        /// Property key representing current operation status text (e.g. "Downloading").
        /// </summary>
        public const string Status = "status";

        /// <summary>
        /// Property key representing operation progress percentage (0..100).
        /// </summary>
        public const string Progress = "progress";

        /// <summary>
        /// Property key representing visual status icon or emoji (e.g. "🚀", "⚡").
        /// </summary>
        public const string Icon = "icon";

        /// <summary>
        /// Property key representing arbitrary tags or labels attached to current context.
        /// </summary>
        public const string Tags = "tags";
    }

    /// <summary>
    /// Well-known context flags.
    /// </summary>
    public enum CallContextProperty
    {
        SuppressStdout,
        LogConfigurationName,
        DisableCache,
        IncludeExternalScopes,
        IncludeCallContext,
        SuppressConsole,
        SuppressedProviders,
        Status,
        Progress,
        Icon,
        Tags
    }
}
