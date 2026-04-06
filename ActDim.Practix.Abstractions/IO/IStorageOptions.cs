using System;

namespace ActDim.Practix.Abstractions.IO
{
    public interface IStorageOptions
    {
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? Ttl { get; set; }
        public DateTimeOffset? SlidingExpiration { get; set; } // "autoRenewOnUse" pattern
    }
}