using System;

namespace ActDim.Practix.BlobManager
{
    public interface IBlobStoreOptions
    {
        DateTimeOffset? AbsoluteExpiration { get; set; }
        TimeSpan? Ttl { get; set; }
        TimeSpan? SlidingExpiration { get; set; }
        string ContentType { get; set; }
        string Hash { get; set; }
        string Metadata { get; set; }
    }
}
