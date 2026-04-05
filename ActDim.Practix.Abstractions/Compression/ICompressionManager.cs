namespace ActDim.Practix.Abstractions.Compression
{
    public interface ICompressionManager
    {
        /// <summary>
        /// Opens the stream for reading (may not be seekable) or writing.
        /// </summary>
        /// <returns>A Stream that represents the contents of the archive entry.</returns>
        /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive. -or- The archive that this entry belongs to has been just created and this entry has already been written to once.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read. -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The archive that this entry belongs to has been disposed.</exception>
        public delegate Stream OpenStreamDelegate();

        // ArchiveEntryReaderAsyncCallback
        public delegate Task<bool> ArchiveEntryReaderAsyncDelegate(IArchiveEntry entry, OpenStreamDelegate openRead);
        // ArchiveEntryWriterAsyncCallback
        public delegate Task<bool> ArchiveEntryWriterAsyncDelegate(IArchiveEntry entry, OpenStreamDelegate openWrite);

        ArchiveFormat GetArchiveFormat(ReadOnlyMemory<byte> data);

        ArchiveFormat GetArchiveFormat(Stream stream);

        CompressionFormat GetCompressionFormat(ReadOnlyMemory<byte> data);

        CompressionFormat GetCompressionFormat(Stream stream);

        Task<Stream> CompressAsync(ReadOnlyMemory<byte> data, CompressionFormat compressionFormat, CancellationToken cancellationToken = default);

        Task<Stream> CompressAsync(Stream stream, CompressionFormat compressionFormat, CancellationToken cancellationToken = default);

        Task<Stream> DecompressAsync(ReadOnlyMemory<byte> data, CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default);

        Task<byte[]> DecompressAsync(Stream stream, CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default);

        Task DecompressAsync(ReadOnlyMemory<byte> data, Stream outputStream, CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default);

        Task DecompressAsync(Stream stream, Stream outputStream, CompressionFormat? compressionFormat = default, CancellationToken cancellationToken = default);

        Task DecompressArchiveAsync(ReadOnlyMemory<byte> data, ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default);

        Task DecompressArchiveAsync(Stream stream, ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat = default, CancellationToken cancellationToken = default);

        // Task<IArchiveEntry> CreateArchiveEntry
        // Contents
        Task<IList<IArchiveEntry>> GetArchiveEntriesAsync(Stream stream,
            ArchiveFormat? archiveFormat = default,
            CancellationToken cancellationToken = default);

        Task<IList<IArchiveEntry>> GetArchiveEntriesAsync(ReadOnlyMemory<byte> data,
            ArchiveFormat? archiveFormat = default,
            CancellationToken cancellationToken = default);

        Task<Stream> CompressToArchiveAsync(Stream outputStream,
                IEnumerable<ArchiveEntrySource> sources,
                ArchiveFormat? archiveFormat = default,
                CancellationToken cancellationToken = default);

        Task<Stream> CompressToArchiveAsync(Stream outputStream,
            IEnumerable<ArchiveEntrySource> sources,
            ArchiveEntryWriterAsyncDelegate writer,
            ArchiveFormat? archiveFormat = default,
            CancellationToken cancellationToken = default);

        Task<Stream> CompressToArchiveAsync(IEnumerable<ArchiveEntrySource> sources,
            ArchiveFormat? archiveFormat = default,
            CancellationToken cancellationToken = default);

        Task<Stream> CompressToArchiveAsync(IEnumerable<ArchiveEntrySource> sources,
            ArchiveEntryWriterAsyncDelegate writer,
            ArchiveFormat? archiveFormat = default,
            CancellationToken cancellationToken = default);


        ArchiveFormat GetArchiveFormatByFileExtension(string ext);

        string FixArchiveFileExtension(string fileName, ArchiveFormat? archiveFormat = default);
    }
}