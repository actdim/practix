using System;

namespace ActDim.Observability.Tests.OpenObserve
{
    /// <summary>
    /// Configuration options for OpenObserve log ingestion and SQL query processing in tests.
    /// </summary>
    public sealed class OpenObserveOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:5080";
        public string Organization { get; set; } = "default";
        public string Stream { get; set; } = "actdim";
        public string UserEmail { get; set; } = "root@example.com";
        public string UserPassword { get; set; } = "Complexpass#123";
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(200);
        public int MaxBatchSize { get; set; } = 50;
    }
}
