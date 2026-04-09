namespace ActDim.Practix.Abstractions.Compression
{
    /// <summary>
    /// Archive container formats. These formats can store multiple files and directories, and support optional compression algorithms.
    /// </summary>
    public enum ArchiveFormat
    {
        /// <summary>
        /// ZIP (based on PKWARE spec). Supports multiple files and folders.
        /// Common compression methods: Store (no compression), Deflate, BZip2, LZMA, PPMd, Deflate64.
        /// </summary>
        Zip,

        /// <summary>
        /// 7z (7-Zip format). Highly flexible and extensible container.
        /// Supported compression: LZMA, LZMA2, BZip2, PPMd, Deflate.
        /// </summary>
        SevenZip,

        /// <summary>
        /// RAR (proprietary format). Supports solid compression and error recovery.
        /// Compression algorithms: proprietary (RAR), also supports recovery records.
        /// </summary>
        Rar,

        /// <summary>
        /// TAR (Tape Archive). Simple container for multiple files.
        /// Does not compress by itself; often combined with GZip, BZip2, or XZ (e.g. .tar.gz, .tar.bz2).
        /// </summary>
        Tar
    }
}
