using System.Collections.Generic;
using ActDim.Practix.Abstractions.Compression;

namespace ActDim.Practix.Compression
{
    /// <summary>
    /// Format-agnostic archive descriptor shared by every <see cref="ArchiveEntry"/> of one archive.
    /// <see cref="FileName"/> stays null for a stream-backed archive (there is no file to name), and
    /// <see cref="Entries"/> is filled as the archive is enumerated - for a sequentially read format (TAR)
    /// it therefore only contains the entries seen so far.
    /// </summary>
    public sealed class ArchiveInfo : IArchiveInfo
    {
        /// <inheritdoc/>
        public string FileName { get; set; }

        /// <inheritdoc/>
        public long Size { get; set; }

        /// <inheritdoc/>
        public ICollection<IArchiveEntry> Entries { get; set; }
    }
}
