namespace ActDim.Practix.Abstractions.Compression
{
    /// <summary>
    /// What kind of filesystem object an archive entry represents. Needed to extract an archive correctly:
    /// without it a directory entry is indistinguishable from an empty file, so the directory tree (and any
    /// empty directory) is lost on extraction.
    /// </summary>
    public enum ArchiveEntryType
    {
        /// <summary>
        /// An ordinary file with a data section. The default, and the only kind every archive format supports.
        /// </summary>
        RegularFile,

        /// <summary>
        /// A directory. Carries no data. In ZIP this is stored by convention as an entry whose name ends with a
        /// forward slash; in TAR it is an explicit entry type.
        /// </summary>
        Directory,

        /// <summary>
        /// A symbolic link; the target path is in <see cref="IArchiveEntry.LinkTarget"/>. TAR only - ZIP has no
        /// portable representation for it.
        /// </summary>
        SymbolicLink,

        /// <summary>
        /// A hard link to another entry of the same archive; the target path is in
        /// <see cref="IArchiveEntry.LinkTarget"/>. TAR only.
        /// </summary>
        HardLink,

        /// <summary>
        /// Anything else the format may carry - character/block devices, FIFOs, metadata-only pseudo entries
        /// (such as TAR extended-attribute blocks). Not extractable as a plain file or directory.
        /// </summary>
        Other
    }
}
