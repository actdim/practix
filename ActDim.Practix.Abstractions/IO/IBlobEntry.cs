using System;
using System.Collections.Generic;

namespace ActDim.Practix.Abstractions.IO
{
    /// <summary>
    /// IBlobMetadata
    /// </summary>
    public interface IBlobEntry
    {
        public DateTimeOffset CreatedAt { get; set; }

        // /// <summary>
        // /// LastModified
        // /// </summary>
        // public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// LastAccessed/LastAccessTime
        /// </summary>
        public DateTimeOffset AccessedAt { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// AbsoluteExpiration
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        public string ContentType { get; set; }

        public long? Size { get; set; }

        public Dictionary<string, string> Tags { get; set; }
    }
}
