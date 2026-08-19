using System;

namespace ActDim.Observability.VictoriaLogs
{
    /// <summary>
    /// Configuration options for VictoriaLogs ingestion, stream labeling, and LogsQL queries.
    /// </summary>
    public sealed class VictoriaLogsOptions
    {
        /// <summary>
        /// Gets or sets the VictoriaLogs server base URL. Defaults to <c>http://localhost:9428</c>.
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:9428";

        /// <summary>
        /// Gets or sets the stream labels in VictoriaLogs stream syntax (e.g. <c>{app="actdim",env="test"}</c>).
        /// </summary>
        public string Stream { get; set; } = "{app=\"actdim\"}";

        /// <summary>
        /// Gets or sets the batching flush interval for transmitting log entries. Defaults to 200ms.
        /// </summary>
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Gets or sets maximum batch size before triggering an immediate flush. Defaults to 50 records.
        /// </summary>
        public int MaxBatchSize { get; set; } = 50;
    }
}
