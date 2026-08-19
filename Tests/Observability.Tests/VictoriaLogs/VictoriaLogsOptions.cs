using System;

namespace ActDim.Observability.Tests.VictoriaLogs
{
    /// <summary>
    /// Configuration options for VictoriaLogs ingestion, stream labeling, and LogsQL queries in tests.
    /// </summary>
    public sealed class VictoriaLogsOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:9428";
        public string Stream { get; set; } = "{app=\"actdim\"}";
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(200);
        public int MaxBatchSize { get; set; } = 50;
    }
}
