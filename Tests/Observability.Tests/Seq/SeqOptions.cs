using System;

namespace ActDim.Observability.Tests.Seq
{
    /// <summary>
    /// Configuration options for Seq server connection and ingestion.
    /// </summary>
    public sealed class SeqOptions
    {
        /// <summary>
        /// Gets or sets the base URL of the Seq instance (default: <c>http://localhost:5341</c>).
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:5341";

        /// <summary>
        /// Gets or sets the optional Seq API key.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the batch dispatch interval.
        /// </summary>
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(50);
    }
}
