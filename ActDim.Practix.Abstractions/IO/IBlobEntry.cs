namespace ActDim.Practix.Abstractions.IO
{
    public interface IBlobEntry // IBlobMetadata
    {
        public DateTimeOffset CreatedAt { get; set; }

        // public DateTimeOffset UpdatedAt { get; set; } // LastModified

        public DateTimeOffset AccessedAt { get; set; } // LastAccessed/LastAccessTime

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