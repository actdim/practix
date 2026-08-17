using System;

namespace ActDim.BlobManager
{
    public class BlobStoreOptions
    {
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? Ttl { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public string ContentType { get; set; }
        public string Hash { get; set; }
        public string Metadata { get; set; }
    }
}
