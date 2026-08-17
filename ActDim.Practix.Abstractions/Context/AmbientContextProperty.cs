namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Well-known ambient execution context flags.
    /// </summary>
    /// <remarks>
    /// Telemetry-specific ambient properties intentionally do not live here: they belong to the observability context
    /// of the <c>ActDim.Observability</c> package, while <see cref="IAmbientContext"/> stays a neutral ambient
    /// variable store with no telemetry meaning.
    /// </remarks>
    public enum AmbientContextProperty
    {
        SuppressStdout,
        LogConfigurationName,
        DisableCache
    }
}
