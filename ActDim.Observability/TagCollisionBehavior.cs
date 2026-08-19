
namespace ActDim.Observability
{
    /// <summary>
    /// Defines how <see cref="EventObservabilityBridge"/> resolves a telemetry tag written more than once
    /// within a single observability write.
    /// </summary>
    public enum TagCollisionBehavior
    {
        /// <summary>
        /// Keeps the value written first and discards subsequent ones. Default.
        /// </summary>
        KeepFirst,

        /// <summary>
        /// Replaces the previously written value with the latest one.
        /// </summary>
        Overwrite,

        /// <summary>
        /// Throws an <see cref="System.InvalidOperationException"/> on the first collision.
        /// Intended for tests and CI, where silent telemetry loss must fail the build.
        /// </summary>
        Throw
    }
}
