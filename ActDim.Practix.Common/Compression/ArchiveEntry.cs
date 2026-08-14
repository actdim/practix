using System;
using ActDim.Practix.Abstractions.Compression;

namespace ActDim.Practix.Compression
{
    /// <summary>
    /// Format-agnostic archive entry descriptor. One instance is produced per physical archive entry, so a
    /// callback may safely keep the reference after the enumeration moved on (the entry DATA stream, however,
    /// is only valid inside the callback - see <see cref="CompressionManager.DecompressArchiveAsync(System.IO.Stream, ICompressionManager.ArchiveEntryReaderAsyncDelegate, ArchiveFormat?, System.Threading.CancellationToken)"/>).
    /// </summary>
    /// <inheritdoc />
    public sealed class ArchiveEntry : IArchiveEntry
    {
        /// <inheritdoc/>
        public string FullName { get; set; }

        /// <inheritdoc/>
        public long Size { get; set; }

        /// <inheritdoc/>
        public ArchiveEntryType EntryType { get; set; }

        /// <inheritdoc/>
        public DateTimeOffset? LastWriteTime { get; set; }

        /// <inheritdoc/>
        public long? CompressedSize { get; set; }

        /// <inheritdoc/>
        public string LinkTarget { get; set; }

        /// <inheritdoc/>
        public IArchiveInfo ArchiveInfo { get; set; }
    }
}
