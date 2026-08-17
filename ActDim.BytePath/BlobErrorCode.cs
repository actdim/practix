namespace ActDim.BytePath
{
    /// <summary>
    /// Represents the status error codes for blob retrieval or operation results.
    /// </summary>
    public enum BlobErrorCode
    {
        /// <summary>
        /// Operation completed successfully with no errors.
        /// </summary>
        None = 0,

        /// <summary>
        /// The requested blob key was not found.
        /// </summary>
        KeyNotFound,

        /// <summary>
        /// Lock acquisition for the requested blob timed out.
        /// </summary>
        Timeout,
    }
}
