using System;

namespace ActDim.Practix.Abstractions.Compression
{
    public interface IArchiveEntry
    {
        /// <summary>
        /// The relative path of the entry as stored in the archive. Note that archives allow any string to be the path of the entry, including invalid and absolute paths.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// The uncompressed size of the entry. This property is not valid in Create mode, and it is only valid in Update mode if the entry has not been opened.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is not available because the entry has been written to or modified.</exception>
        public long Size { get; set; }

        /// <summary>
        /// What the entry represents. Anything other than <see cref="ArchiveEntryType.RegularFile"/> has no data
        /// section, so opening its stream yields an empty stream (ZIP) or null (TAR) - check this instead of
        /// inferring "empty file" from <see cref="Size"/> being 0.
        /// </summary>
        public ArchiveEntryType EntryType { get; set; }

        /// <summary>
        /// Last modification time recorded in the archive, or null when the format did not store one.
        /// Required to restore timestamps on extraction.
        /// <para>
        /// Precision and time zone are format-dependent, and the difference is observable: a ZIP entry carries
        /// a timezone-less DOS wall clock with 2-second resolution, so only the wall-clock value is meaningful
        /// (the offset reported back is the reading machine's local one). A TAR (PAX) entry carries a Unix
        /// timestamp and round-trips exactly as UTC.
        /// </para>
        /// </summary>
        public DateTimeOffset? LastWriteTime { get; set; }

        /// <summary>
        /// The size this entry occupies inside the archive, or null when the format does not track it
        /// per entry (TAR stores its entries uncompressed, so the container as a whole is what gets compressed).
        /// Reporting only - <see cref="Size"/> is what an extraction needs.
        /// </summary>
        public long? CompressedSize { get; set; }

        /// <summary>
        /// For <see cref="ArchiveEntryType.SymbolicLink"/> and <see cref="ArchiveEntryType.HardLink"/>, the path
        /// the link points at; null for every other entry type.
        /// </summary>
        public string LinkTarget { get; set; }

        public IArchiveInfo ArchiveInfo { get; set; }
    }
}
