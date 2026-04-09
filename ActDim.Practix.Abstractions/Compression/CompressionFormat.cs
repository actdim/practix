namespace ActDim.Practix.Abstractions.Compression
{
    /// <summary>
    /// Stream compression formats. These formats compress a byte stream without file-level metadata or structure.
    /// </summary>
    public enum CompressionFormat
    {
        /// <summary>
        /// GZip (RFC 1952). Uses Deflate compression internally.
        /// Includes minimal metadata (original filename, timestamp, CRC32).
        /// </summary>
        GZip,

        /// <summary>
        /// Brotli. Modern compression format optimized for web use.
        /// Includes internal framing and metadata.
        /// </summary>
        Brotli,

        /// <summary>
        /// Deflate (RFC 1951) or Zlib (RFC 1950, used in .NET).
        /// Raw compression without headers or container.
        /// </summary>
        Deflate,

        /// <summary>
        /// BZip2. Block-sorting compression, good for text data.
        /// No container structure, just compressed stream.
        /// </summary>
        BZip2,

        /// <summary>
        /// LZMA. High compression ratio, used in 7z and other formats.
        /// Requires external framing or container for file metadata.
        /// </summary>
        LZMA,

        /// <summary>
        /// LZMA2. Improved version of LZMA with better multithreading and chunking.
        /// Used primarily inside 7z containers.
        /// </summary>
        LZMA2,

        /// <summary>
        /// PPMd. Prediction by Partial Matching, optimized for text compression.
        /// Used in advanced containers like 7z.
        /// </summary>
        PPMd
    }
}
