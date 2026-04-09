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

        public IArchiveInfo ArchiveInfo { get; set; }
    }
}
