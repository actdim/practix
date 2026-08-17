using System;

namespace ActDim.BlobManager
{
    /// <summary>
    /// Configuration options and metadata instructions applied when storing or mutating a blob record.
    /// </summary>
    public class BlobStoreOptions
    {
        /// <summary>
        /// Gets or sets an explicit absolute timestamp when the blob expires.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets a relative time-to-live duration from the moment of storage.
        /// </summary>
        public TimeSpan? Ttl { get; set; }

        /// <summary>
        /// Gets or sets a sliding expiration window that refreshes on each access.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the MIME content type of the blob data (e.g. "application/json").
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets a checksum or content hash value.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets custom JSON or text metadata associated with the blob.
        /// </summary>
        public string Metadata { get; set; }
    }
}
