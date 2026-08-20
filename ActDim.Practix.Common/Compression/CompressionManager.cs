using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Abstractions.Exceptions;
using ActDim.Practix.Extensions;
using ActDim.Practix.Common.Memory;
using Ardalis.GuardClauses;

namespace ActDim.Practix.Compression
{
    /// <summary>
    /// Compression / archiving facade built on the .NET 10 base class library only (no third-party codec).
    /// <para>
    /// Design rules that hold for every method here:
    /// </para>
    /// <list type="bullet">
    /// <item>No payload ever lands on the managed heap: temporary streams come from
    /// <see cref="MemoryManager.Default"/> (a pooled <c>RecyclableMemoryStream</c>), copies go through
    /// <see cref="StreamExtensions"/> / <see cref="Stream.CopyToAsync(Stream, int, CancellationToken)"/>
    /// which rent their buffers from <see cref="System.Buffers.ArrayPool{T}"/>, and format sniffing reads
    /// into a stack-allocated span.</item>
    /// <item>No <see cref="BufferedStream"/> wrapper around the codec streams: <see cref="GZipStream"/>,
    /// <see cref="DeflateStream"/> and <see cref="BrotliStream"/> already buffer internally, so wrapping them
    /// only adds an allocation plus one extra copy per block.</item>
    /// <item>An input stream is always consumed as a WHOLE: a seekable input is rewound to 0 first.</item>
    /// <item>A stream this class CREATES is returned rewound to 0 and is owned by the caller (dispose it -
    /// that returns the pooled blocks). A stream the CALLER passes as the destination is never rewound and
    /// never closed, so writing into it can be composed / appended.</item>
    /// <item>Entry streams handed to a reader/writer callback are owned by this class and are disposed as soon
    /// as the callback returns - a callback must consume (or fully write) them before it completes.</item>
    /// </list>
    /// <para>
    /// Format coverage is bounded by the BCL: <see cref="CompressionFormat.GZip"/>,
    /// <see cref="CompressionFormat.Deflate"/> (raw RFC 1951) and <see cref="CompressionFormat.Brotli"/> for
    /// streams; <see cref="ArchiveFormat.Zip"/> and <see cref="ArchiveFormat.Tar"/> for archives. Everything
    /// else (BZip2, LZMA, LZMA2, PPMd, 7z, RAR) is detected by signature but throws
    /// <see cref="NotSupportedException"/> on use, because the BCL ships no codec for it.
    /// </para>
    /// </summary>
    /// <inheritdoc />
    [Obfuscation(Exclude = true)]
    public class CompressionManager : ICompressionManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionManager"/> class.
        /// </summary>
        public CompressionManager()
        {
        }

        /// <summary>
        /// Compression level used wherever the public API does not carry one (the
        /// <see cref="ICompressionManager"/> contract never does). Override to trade ratio for speed.
        /// </summary>
        protected virtual CompressionLevel DefaultCompressionLevel
        {
            get
            {
                return CompressionLevel.Optimal;
            }
        }

        /// <summary>
        /// Archive format assumed when a WRITE method is called without one.
        /// </summary>
        protected virtual ArchiveFormat DefaultArchiveFormat
        {
            get
            {
                return ArchiveFormat.Zip;
            }
        }

        /// <summary>
        /// Copy buffer size, in bytes. Buffers of this size are rented from
        /// <see cref="System.Buffers.ArrayPool{T}"/>, so it is a pooling bucket hint, not an allocation.
        /// 80 kB is the BCL default for <see cref="Stream.CopyTo(Stream)"/> and stays below the
        /// large-object-heap threshold.
        /// </summary>
        protected virtual int BufferSize
        {
            get
            {
                return 81920;
            }
        }

        /// <summary>
        /// Creates the scratch stream used whenever a method has to materialize a payload it must hand back
        /// or re-read. Pooled and seekable; the owner disposes it.
        /// </summary>
        protected virtual Stream CreateTempStream()
        {
            return MemoryManager.Default.GetStream(nameof(CompressionManager));
        }

        // File signatures - https://en.wikipedia.org/wiki/List_of_file_signatures
        // Declared as ReadOnlySpan<byte> properties on purpose: a collection expression / u8 literal of
        // constants compiles to a span over static read-only data, so reading them allocates nothing
        // (a static byte[] field would be one heap array plus a mutable-content hazard).

        private static ReadOnlySpan<byte> GZipSignature => [0x1F, 0x8B];

        private static ReadOnlySpan<byte> BZip2Signature => "BZh"u8;

        // LZMA "alone" header: properties byte 0x5D followed by a 4-byte dictionary size, of which the two
        // low bytes are 0x00 for every dictionary size produced in practice.
        private static ReadOnlySpan<byte> LzmaSignature => [0x5D, 0x00, 0x00];

        private static ReadOnlySpan<byte> ZipLocalFileHeaderSignature => [0x50, 0x4B, 0x03, 0x04];

        // An empty archive carries only the end-of-central-directory record.
        private static ReadOnlySpan<byte> ZipEmptyArchiveSignature => [0x50, 0x4B, 0x05, 0x06];

        private static ReadOnlySpan<byte> ZipSpannedArchiveSignature => [0x50, 0x4B, 0x07, 0x08];

        private static ReadOnlySpan<byte> SevenZipSignature => [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C];

        private static ReadOnlySpan<byte> RarSignature => [0x52, 0x61, 0x72, 0x21, 0x1A, 0x07];

        // TAR has no signature at offset 0; the POSIX/ustar magic sits inside the first header block.
        private static ReadOnlySpan<byte> TarMagic => "ustar"u8;

        private const int TarMagicOffset = 257;

        private const int SignatureBufferLength = TarMagicOffset + 8;

        /// <inheritdoc/>
        public ArchiveFormat GetArchiveFormat(ReadOnlyMemory<byte> data)
        {
            if (!TryDetectArchiveFormat(data.Span, out var archiveFormat))
            {
                throw UnknownArchiveFormat();
            }
            return archiveFormat;
        }

        /// <inheritdoc/>
        public ArchiveFormat GetArchiveFormat(Stream stream)
        {
            Span<byte> header = stackalloc byte[SignatureBufferLength];
            var length = ReadSignature(stream, header);
            if (!TryDetectArchiveFormat(header.Slice(0, length), out var archiveFormat))
            {
                throw UnknownArchiveFormat();
            }
            return archiveFormat;
        }

        /// <inheritdoc/>
        public CompressionFormat GetCompressionFormat(ReadOnlyMemory<byte> data)
        {
            if (!TryDetectCompressionFormat(data.Span, out var compressionFormat))
            {
                throw UnknownCompressionFormat();
            }
            return compressionFormat;
        }

        /// <inheritdoc/>
        public CompressionFormat GetCompressionFormat(Stream stream)
        {
            Span<byte> header = stackalloc byte[SignatureBufferLength];
            var length = ReadSignature(stream, header);
            if (!TryDetectCompressionFormat(header.Slice(0, length), out var compressionFormat))
            {
                throw UnknownCompressionFormat();
            }
            return compressionFormat;
        }

        /// <summary>
        /// Reads the leading bytes of <paramref name="stream"/> into <paramref name="header"/> (typically a
        /// stack-allocated span) and restores the original position. Returns how many bytes were actually
        /// available.
        /// </summary>
        private static int ReadSignature(Stream stream, Span<byte> header)
        {
            Guard.Against.Null(stream, nameof(stream));

            if (!stream.CanSeek)
            {
                throw new NotSupportedException("Format detection needs a seekable stream: a non-seekable stream cannot be rewound after its header was consumed. Buffer it (Stream.ToMemoryAsync) or pass the format explicitly.");
            }

            var position = stream.Position;
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                // Reads up to header.Length bytes; a shorter stream is not an error here, the caller matches
                // signatures against what was available.
                return stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
            }
            finally
            {
                stream.Position = position;
            }
        }

        private static bool TryDetectCompressionFormat(ReadOnlySpan<byte> header, out CompressionFormat compressionFormat)
        {
            // Brotli and raw Deflate are headerless by design and are therefore NOT detectable - they can
            // only be decompressed with an explicitly passed format.
            if (header.StartsWith(GZipSignature))
            {
                compressionFormat = CompressionFormat.GZip;
                return true;
            }
            if (header.StartsWith(BZip2Signature))
            {
                compressionFormat = CompressionFormat.BZip2;
                return true;
            }
            if (header.StartsWith(LzmaSignature))
            {
                compressionFormat = CompressionFormat.LZMA;
                return true;
            }
            compressionFormat = default;
            return false;
        }

        private static bool TryDetectArchiveFormat(ReadOnlySpan<byte> header, out ArchiveFormat archiveFormat)
        {
            if (header.StartsWith(ZipLocalFileHeaderSignature)
                || header.StartsWith(ZipEmptyArchiveSignature)
                || header.StartsWith(ZipSpannedArchiveSignature))
            {
                archiveFormat = ArchiveFormat.Zip;
                return true;
            }
            if (header.StartsWith(SevenZipSignature))
            {
                archiveFormat = ArchiveFormat.SevenZip;
                return true;
            }
            if (header.StartsWith(RarSignature))
            {
                archiveFormat = ArchiveFormat.Rar;
                return true;
            }
            // Only POSIX/ustar and GNU tar can be recognized; the original V7 format carries no magic at all.
            if (header.Length >= TarMagicOffset + TarMagic.Length
                && header.Slice(TarMagicOffset, TarMagic.Length).SequenceEqual(TarMagic))
            {
                archiveFormat = ArchiveFormat.Tar;
                return true;
            }
            archiveFormat = default;
            return false;
        }

        private static Exception UnknownCompressionFormat()
        {
            return new DataFormatException("Unrecognized compression format. Note that Brotli and raw Deflate carry no header and can never be detected - pass the format explicitly for those.");
        }

        private static Exception UnknownArchiveFormat()
        {
            return new DataFormatException("Unrecognized archive format.");
        }

        private static Exception UnsupportedCompressionFormat(CompressionFormat compressionFormat)
        {
            return new NotSupportedException($"Unsupported {nameof(CompressionFormat)}.{compressionFormat}: the .NET base class library ships no codec for it. Supported: {nameof(CompressionFormat.GZip)}, {nameof(CompressionFormat.Deflate)}, {nameof(CompressionFormat.Brotli)}.");
        }

        private static Exception UnsupportedArchiveFormat(ArchiveFormat archiveFormat)
        {
            return new NotSupportedException($"Unsupported {nameof(ArchiveFormat)}.{archiveFormat}: the .NET base class library ships no reader/writer for it. Supported: {nameof(ArchiveFormat.Zip)}, {nameof(ArchiveFormat.Tar)}.");
        }

        /// <summary>
        /// Wraps <paramref name="outputStream"/> in an encoder. The encoder is left leaveOpen: disposing it
        /// flushes the codec trailer without closing the destination.
        /// </summary>
        private Stream CreateCompressionStream(Stream outputStream, CompressionFormat compressionFormat)
        {
            switch (compressionFormat)
            {
                case CompressionFormat.GZip:
                    return new GZipStream(outputStream, DefaultCompressionLevel, leaveOpen: true);
                case CompressionFormat.Deflate:
                    // Raw deflate (RFC 1951). Use ZLibStream if an RFC 1950 header is required.
                    return new DeflateStream(outputStream, DefaultCompressionLevel, leaveOpen: true);
                case CompressionFormat.Brotli:
                    return new BrotliStream(outputStream, DefaultCompressionLevel, leaveOpen: true);
                default:
                    throw UnsupportedCompressionFormat(compressionFormat);
            }
        }

        /// <summary>
        /// Wraps <paramref name="inputStream"/> in a decoder that streams (it never materializes the whole
        /// payload) and leaves the source open.
        /// </summary>
        private static Stream CreateDecompressionStream(Stream inputStream, CompressionFormat compressionFormat)
        {
            switch (compressionFormat)
            {
                case CompressionFormat.GZip:
                    return new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
                case CompressionFormat.Deflate:
                    return new DeflateStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
                case CompressionFormat.Brotli:
                    return new BrotliStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
                default:
                    throw UnsupportedCompressionFormat(compressionFormat);
            }
        }

        /// <summary>
        /// Exposes <paramref name="data"/> as a readable stream without copying whenever it is array-backed
        /// (the common case: <c>byte[]</c>, <c>ArraySegment</c>, a slice of either).
        /// </summary>
        private Stream CreateReadStream(ReadOnlyMemory<byte> data)
        {
            if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment) && segment.Array != null)
            {
                // publiclyVisible: true keeps TryGetBuffer working, which is what lets the StreamExtensions
                // fast paths write the payload in a single call.
                return new MemoryStream(segment.Array, segment.Offset, segment.Count, writable: false, publiclyVisible: true);
            }

            // Not array-backed (native / MemoryManager-backed memory): one pooled copy, no heap array.
            var stream = CreateTempStream();
            stream.Write(data.Span);
            stream.Position = 0L;
            return stream;
        }

        private static void Rewind(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0L;
            }
        }

        /// <summary>
        /// Copies the whole source into the destination. Prefers the <see cref="StreamExtensions"/> memory
        /// fast path (one single write straight from the exposed buffer) and otherwise falls back to
        /// <see cref="Stream.CopyToAsync(Stream, int, CancellationToken)"/>, which rents its buffer from
        /// <see cref="System.Buffers.ArrayPool{T}"/> and streams block by block. The fallback is used instead
        /// of <see cref="StreamExtensions.ZeroAllocCopyToAsync{TStream}(Stream, TStream, int, CancellationToken)"/>
        /// on purpose: for a NON-seekable source that helper buffers the entire payload into memory first,
        /// which would defeat streaming (decoder output can be orders of magnitude larger than its input).
        /// </summary>
        private async Task CopyAllAsync(Stream src, Stream dst, CancellationToken cancellationToken)
        {
            if (src is MemoryStream memoryStream)
            {
                await memoryStream.ZeroAllocCopyToAsync(dst, BufferSize, cancellationToken);
                return;
            }

            if (src.CanSeek)
            {
                src.Seek(0, SeekOrigin.Begin);
            }

            await src.CopyToAsync(dst, BufferSize, cancellationToken);
        }

        /// <inheritdoc/>
        /// <returns>A pooled, rewound stream holding the compressed payload; the caller must dispose it.</returns>
        public async Task<Stream> CompressAsync(ReadOnlyMemory<byte> data, CompressionFormat compressionFormat, CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            try
            {
                await CompressAsync(data, outputStream, compressionFormat, cancellationToken);
                Rewind(outputStream);
                return outputStream;
            }
            catch
            {
                // Never leak the pooled blocks of a stream the caller will not receive.
                await outputStream.DisposeAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        /// <returns>A pooled, rewound stream holding the compressed payload; the caller must dispose it.</returns>
        public async Task<Stream> CompressAsync(Stream stream, CompressionFormat compressionFormat, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");

            var outputStream = CreateTempStream();
            try
            {
                await CompressAsync(stream, outputStream, compressionFormat, cancellationToken);
                Rewind(outputStream);
                return outputStream;
            }
            catch
            {
                await outputStream.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Compresses <paramref name="data"/> straight into <paramref name="outputStream"/> - the allocation-free
        /// overload: nothing is materialized, not even a scratch stream.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outputStream">Destination. Written from its current position; left open and NOT rewound.</param>
        /// <param name="compressionFormat"></param>
        /// <param name="cancellationToken"></param>
        public async Task CompressAsync(ReadOnlyMemory<byte> data, Stream outputStream, CompressionFormat compressionFormat, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(outputStream, nameof(outputStream));

            await using var compressionStream = CreateCompressionStream(outputStream, compressionFormat);

            await compressionStream.WriteAsync(data, cancellationToken);
        }

        /// <summary>
        /// Compresses the whole <paramref name="stream"/> straight into <paramref name="outputStream"/> - the
        /// allocation-free overload: nothing is materialized, not even a scratch stream.
        /// </summary>
        /// <param name="stream">Source. Read as a whole (rewound first when seekable); left open.</param>
        /// <param name="outputStream">Destination. Written from its current position; left open and NOT rewound.</param>
        /// <param name="compressionFormat"></param>
        /// <param name="cancellationToken"></param>
        public async Task CompressAsync(Stream stream, Stream outputStream, CompressionFormat compressionFormat, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");
            Guard.Against.Null(outputStream, nameof(outputStream));

            await using var compressionStream = CreateCompressionStream(outputStream, compressionFormat);

            await CopyAllAsync(stream, compressionStream, cancellationToken);
        }

        /// <inheritdoc/>
        /// <returns>A pooled, rewound stream holding the decompressed payload; the caller must dispose it.</returns>
        public async Task<Stream> DecompressAsync(ReadOnlyMemory<byte> data, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            try
            {
                await DecompressAsync(data, outputStream, compressionFormat, cancellationToken);
                Rewind(outputStream);
                return outputStream;
            }
            catch
            {
                await outputStream.DisposeAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DecompressAsync(ReadOnlyMemory<byte> data, Stream outputStream, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(outputStream, nameof(outputStream));

            // Sniff the span directly - no stream, no rewind, no copy.
            var format = compressionFormat ?? GetCompressionFormat(data);

            await using var inputStream = CreateReadStream(data);

            await DecompressCoreAsync(inputStream, outputStream, format, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DecompressAsync(Stream stream, Stream outputStream, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");
            Guard.Against.Null(outputStream, nameof(outputStream), "Invalid input");

            if (compressionFormat == null && !stream.CanSeek)
            {
                // Detection has to read - and then un-read - the header, so the input must be seekable.
                // Buffering goes through the pooled recyclable stream, no heap array involved.
                using var seekableStream = await stream.ToMemoryAsync(cancellationToken);

                await DecompressCoreAsync(seekableStream, outputStream, GetCompressionFormat(seekableStream), cancellationToken);
                return;
            }

            await DecompressCoreAsync(stream, outputStream, compressionFormat ?? GetCompressionFormat(stream), cancellationToken);
        }

        private async Task DecompressCoreAsync(Stream stream, Stream outputStream, CompressionFormat compressionFormat, CancellationToken cancellationToken)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            // The decoder is not seekable, so CopyToAsync (ArrayPool-backed, and overridden by the codec
            // streams with an even tighter loop) is used directly instead of CopyAllAsync.
            await using var decompressionStream = CreateDecompressionStream(stream, compressionFormat);

            await decompressionStream.CopyToAsync(outputStream, BufferSize, cancellationToken);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This overload is the one place where a heap array is unavoidable - it is the declared return type.
        /// Prefer <see cref="DecompressAsync(Stream, Stream, CompressionFormat?, CancellationToken)"/> (write
        /// straight into your own destination) on a hot path.
        /// </remarks>
        public async Task<byte[]> DecompressAsync(Stream stream, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");

            using var outputStream = CreateTempStream();

            await DecompressAsync(stream, outputStream, compressionFormat, cancellationToken);

            if (outputStream.Length > int.MaxValue)
            {
                throw new NotSupportedException("Decompressed payload is too long for a single array");
            }

            var length = (int)outputStream.Length;
            if (length == 0)
            {
                return [];
            }

            // The array is filled completely right below, so skip the zeroing pass.
            var result = GC.AllocateUninitializedArray<byte>(length);
            outputStream.Position = 0L;
            outputStream.ReadExactly(result, 0, length);
            return result;
        }

        /// <summary>
        /// Compresses <paramref name="data"/> and hands the result back in a POOLED buffer instead of a heap
        /// array - the zero-alloc counterpart of
        /// <see cref="DecompressAsync(Stream, CompressionFormat?, CancellationToken)"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionFormat"></param>
        /// <param name="ownerFactory">Creates the destination buffer owner; defaults to a pooled
        /// <c>ArrayPoolBufferOwner</c>. See <see cref="StreamExtensions.ReadBytesAsync"/> for the contract.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The compressed bytes; the caller MUST dispose the owner to return the buffer to the pool.</returns>
        public async Task<IBufferOwner<byte>> CompressToBytesAsync(ReadOnlyMemory<byte> data, CompressionFormat compressionFormat, Func<int, IBufferOwner<byte>> ownerFactory = null, CancellationToken cancellationToken = default)
        {
            using var outputStream = CreateTempStream();

            await CompressAsync(data, outputStream, compressionFormat, cancellationToken);
            return await outputStream.ReadBytesAsync(ownerFactory, cancellationToken);
        }

        /// <summary>
        /// Compresses the whole <paramref name="stream"/> and hands the result back in a POOLED buffer instead
        /// of a heap array.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="compressionFormat"></param>
        /// <param name="ownerFactory">Creates the destination buffer owner; defaults to a pooled
        /// <c>ArrayPoolBufferOwner</c>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The compressed bytes; the caller MUST dispose the owner to return the buffer to the pool.</returns>
        public async Task<IBufferOwner<byte>> CompressToBytesAsync(Stream stream, CompressionFormat compressionFormat, Func<int, IBufferOwner<byte>> ownerFactory = null, CancellationToken cancellationToken = default)
        {
            using var outputStream = CreateTempStream();

            await CompressAsync(stream, outputStream, compressionFormat, cancellationToken);
            return await outputStream.ReadBytesAsync(ownerFactory, cancellationToken);
        }

        /// <summary>
        /// Decompresses <paramref name="data"/> and hands the result back in a POOLED buffer instead of a heap
        /// array.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionFormat">Detected from the payload header when null.</param>
        /// <param name="ownerFactory">Creates the destination buffer owner; defaults to a pooled
        /// <c>ArrayPoolBufferOwner</c>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The decompressed bytes; the caller MUST dispose the owner to return the buffer to the pool.</returns>
        public async Task<IBufferOwner<byte>> DecompressToBytesAsync(ReadOnlyMemory<byte> data, CompressionFormat? compressionFormat = null, Func<int, IBufferOwner<byte>> ownerFactory = null, CancellationToken cancellationToken = default)
        {
            using var outputStream = CreateTempStream();

            await DecompressAsync(data, outputStream, compressionFormat, cancellationToken);
            return await outputStream.ReadBytesAsync(ownerFactory, cancellationToken);
        }

        /// <summary>
        /// Decompresses the whole <paramref name="stream"/> and hands the result back in a POOLED buffer
        /// instead of a heap array.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="compressionFormat">Detected from the payload header when null.</param>
        /// <param name="ownerFactory">Creates the destination buffer owner; defaults to a pooled
        /// <c>ArrayPoolBufferOwner</c>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The decompressed bytes; the caller MUST dispose the owner to return the buffer to the pool.</returns>
        public async Task<IBufferOwner<byte>> DecompressToBytesAsync(Stream stream, CompressionFormat? compressionFormat = null, Func<int, IBufferOwner<byte>> ownerFactory = null, CancellationToken cancellationToken = default)
        {
            using var outputStream = CreateTempStream();

            await DecompressAsync(stream, outputStream, compressionFormat, cancellationToken);
            return await outputStream.ReadBytesAsync(ownerFactory, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DecompressArchiveAsync(ReadOnlyMemory<byte> data, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            return WithReadStreamAsync(data, reader, archiveFormat, cancellationToken);
        }

        private async Task WithReadStreamAsync(ReadOnlyMemory<byte> data, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat, CancellationToken cancellationToken)
        {
            await using var stream = CreateReadStream(data);

            await DecompressArchiveAsync(stream, reader, archiveFormat, cancellationToken);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The stream handed to <paramref name="reader"/> through <c>openRead</c> is owned by this method and
        /// is disposed as soon as the callback completes, so the callback MUST consume it before returning.
        /// <c>openRead</c> may be called at most once per entry. Returning <see langword="false"/> stops the
        /// enumeration.
        /// <para>
        /// Check <see cref="IArchiveEntry.EntryType"/> before reading: an entry that is not a
        /// <see cref="ArchiveEntryType.RegularFile"/> has no data section, so <c>openRead</c> yields an empty
        /// stream for ZIP and <see langword="null"/> for TAR.
        /// </para>
        /// </remarks>
        public async Task DecompressArchiveAsync(Stream stream, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");
            Guard.Against.Null(reader, nameof(reader));

            // ZIP is read through its central directory (random access), and detection needs to rewind - only
            // an explicitly requested TAR can be consumed straight off a non-seekable stream.
            if (!stream.CanSeek && archiveFormat != ArchiveFormat.Tar)
            {
                using var seekableStream = await stream.ToMemoryAsync(cancellationToken);

                await DecompressArchiveAsync(seekableStream, reader, archiveFormat, cancellationToken);
                return;
            }

            var format = archiveFormat ?? GetArchiveFormat(stream);

            switch (format)
            {
                case ArchiveFormat.Zip:
                    await ReadZipArchiveAsync(stream, reader, cancellationToken);
                    break;
                case ArchiveFormat.Tar:
                    await ReadTarArchiveAsync(stream, reader, cancellationToken);
                    break;
                default:
                    throw UnsupportedArchiveFormat(format);
            }
        }

        private static async Task ReadZipArchiveAsync(Stream stream, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, CancellationToken cancellationToken)
        {
            Rewind(stream);

            await using var archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null, cancellationToken);

            var zipEntries = archive.Entries;
            var entries = CreateEntryList(zipEntries.Count, stream, out var archiveInfo);

            // The whole entry list is materialized up front (ZIP is random access), so a callback sees a
            // complete ArchiveInfo.Entries even for the very first entry.
            for (var i = 0; i < zipEntries.Count; i++)
            {
                entries.Add(CreateEntry(zipEntries[i], archiveInfo));
            }

            // One opener object and one delegate for the whole archive instead of a fresh closure per
            // entry; it also lets this method own (and close) whatever the callback opened.
            var opener = new ZipEntryOpener();
            ICompressionManager.OpenStreamDelegate openRead = opener.Open;

            for (var i = 0; i < zipEntries.Count; i++)
            {
                opener.Reset(zipEntries[i]);
                bool proceed;
                try
                {
                    proceed = await reader(entries[i], openRead);
                }
                finally
                {
                    await opener.CloseAsync();
                }

                if (!proceed)
                {
                    return;
                }
            }
        }

        private static async Task ReadTarArchiveAsync(Stream stream, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, CancellationToken cancellationToken)
        {
            Rewind(stream);

            await using var tarReader = new TarReader(stream, leaveOpen: true);

            var entries = CreateEntryList(0, stream, out var archiveInfo);

            var opener = new TarEntryOpener();
            ICompressionManager.OpenStreamDelegate openRead = opener.Open;

            while (true)
            {
                // copyData: false - the entry data is a window over the archive stream, valid only until
                // the next call. That is exactly the callback contract here, and it avoids a full copy of
                // every entry.
                var tarEntry = await tarReader.GetNextEntryAsync(copyData: false, cancellationToken);
                if (tarEntry == null)
                {
                    break;
                }

                var entry = CreateEntry(tarEntry, archiveInfo);
                entries.Add(entry);

                opener.Reset(tarEntry);
                if (!await reader(entry, openRead))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Creates the entry list plus the shared <see cref="ArchiveInfo"/> the entries point back to.
        /// One small object per entry is deliberate: a callback may keep the entry it was handed, so entries
        /// cannot be pooled/reused. Payload bytes, unlike this metadata, never hit the heap.
        /// </summary>
        private static List<IArchiveEntry> CreateEntryList(int capacity, Stream stream, out ArchiveInfo archiveInfo)
        {
            var entries = new List<IArchiveEntry>(capacity);
            archiveInfo = new ArchiveInfo
            {
                Size = stream != null && stream.CanSeek ? stream.Length : 0L,
                Entries = entries
            };
            return entries;
        }

        /// <summary>
        /// Projects a ZIP entry onto the format-agnostic descriptor. Read mode only - <c>Length</c> and
        /// <c>CompressedLength</c> are not available while an archive is being created.
        /// </summary>
        private static ArchiveEntry CreateEntry(ZipArchiveEntry zipEntry, ArchiveInfo archiveInfo)
        {
            // ZIP has no entry-type field: a directory is stored by convention as a name ending with a
            // separator, which leaves the file-name part empty. That is the same rule the BCL's own extraction
            // code uses to recognize one.
            var isDirectory = string.IsNullOrEmpty(zipEntry.Name);

            return new ArchiveEntry
            {
                FullName = zipEntry.FullName,
                Size = zipEntry.Length,
                EntryType = isDirectory ? ArchiveEntryType.Directory : ArchiveEntryType.RegularFile,
                LastWriteTime = zipEntry.LastWriteTime,
                CompressedSize = zipEntry.CompressedLength,
                // ZIP has no portable representation for links.
                LinkTarget = null,
                ArchiveInfo = archiveInfo
            };
        }

        /// <summary>
        /// Projects a TAR entry onto the format-agnostic descriptor.
        /// </summary>
        private static ArchiveEntry CreateEntry(TarEntry tarEntry, ArchiveInfo archiveInfo)
        {
            var entryType = MapTarEntryType(tarEntry.EntryType);
            var isLink = entryType == ArchiveEntryType.SymbolicLink || entryType == ArchiveEntryType.HardLink;

            return new ArchiveEntry
            {
                FullName = tarEntry.Name,
                Size = tarEntry.Length,
                EntryType = entryType,
                LastWriteTime = tarEntry.ModificationTime,
                // TAR stores entry data verbatim - there is no per-entry compressed size to report (a
                // ".tar.gz" compresses the whole container, not its entries).
                CompressedSize = null,
                // Reading LinkName is only meaningful for a link entry.
                LinkTarget = isLink ? tarEntry.LinkName : null,
                ArchiveInfo = archiveInfo
            };
        }

        private static ArchiveEntryType MapTarEntryType(TarEntryType tarEntryType)
        {
            switch (tarEntryType)
            {
                case TarEntryType.RegularFile:
                case TarEntryType.V7RegularFile:
                case TarEntryType.ContiguousFile:
                    return ArchiveEntryType.RegularFile;
                case TarEntryType.Directory:
                    return ArchiveEntryType.Directory;
                case TarEntryType.SymbolicLink:
                    return ArchiveEntryType.SymbolicLink;
                case TarEntryType.HardLink:
                    return ArchiveEntryType.HardLink;
                default:
                    // Character/block devices, FIFOs, sparse/multi-volume members and the metadata-only PAX
                    // and GNU pseudo entries: not extractable as a plain file or directory.
                    return ArchiveEntryType.Other;
            }
        }

        /// <inheritdoc/>
        public async Task<IList<IArchiveEntry>> GetArchiveEntriesAsync(Stream stream, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");

            if (!stream.CanSeek && archiveFormat != ArchiveFormat.Tar)
            {
                using var seekableStream = await stream.ToMemoryAsync(cancellationToken);

                return await GetArchiveEntriesAsync(seekableStream, archiveFormat, cancellationToken);
            }

            var format = archiveFormat ?? GetArchiveFormat(stream);

            switch (format)
            {
                case ArchiveFormat.Zip:
                    return await GetZipArchiveEntriesAsync(stream, cancellationToken);
                case ArchiveFormat.Tar:
                    return await GetTarArchiveEntriesAsync(stream, cancellationToken);
                default:
                    throw UnsupportedArchiveFormat(format);
            }
        }

        private static async Task<IList<IArchiveEntry>> GetZipArchiveEntriesAsync(Stream stream, CancellationToken cancellationToken)
        {
            Rewind(stream);

            await using var archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null, cancellationToken);

            var zipEntries = archive.Entries;
            var entries = CreateEntryList(zipEntries.Count, stream, out var archiveInfo);
            for (var i = 0; i < zipEntries.Count; i++)
            {
                entries.Add(CreateEntry(zipEntries[i], archiveInfo));
            }
            return entries;
        }

        private static async Task<IList<IArchiveEntry>> GetTarArchiveEntriesAsync(Stream stream, CancellationToken cancellationToken)
        {
            Rewind(stream);

            await using var tarReader = new TarReader(stream, leaveOpen: true);

            var entries = CreateEntryList(0, stream, out var archiveInfo);
            while (true)
            {
                var tarEntry = await tarReader.GetNextEntryAsync(copyData: false, cancellationToken);
                if (tarEntry == null)
                {
                    break;
                }
                entries.Add(CreateEntry(tarEntry, archiveInfo));
            }
            return entries;
        }

        /// <inheritdoc/>
        public async Task<IList<IArchiveEntry>> GetArchiveEntriesAsync(ReadOnlyMemory<byte> data, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            await using var stream = CreateReadStream(data);

            return await GetArchiveEntriesAsync(stream, archiveFormat, cancellationToken);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Each <see cref="ArchiveEntrySource.OpenReadAsync"/> stream is treated as opened on our behalf and
        /// is disposed once its entry has been written.
        /// </remarks>
        public async Task<Stream> CompressToArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(outputStream, nameof(outputStream));
            Guard.Against.Null(sources, nameof(sources));

            var format = archiveFormat ?? DefaultArchiveFormat;

            switch (format)
            {
                case ArchiveFormat.Zip:
                    await WriteZipArchiveAsync(outputStream, sources, cancellationToken);
                    break;
                case ArchiveFormat.Tar:
                    await WriteTarArchiveAsync(outputStream, sources, cancellationToken);
                    break;
                default:
                    throw UnsupportedArchiveFormat(format);
            }

            // The archive is handed back as a readable stream, so rewind it (unlike the plain
            // compress-into-my-stream overloads, this method's contract is to RETURN the stream).
            Rewind(outputStream);
            return outputStream;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The stream handed to <paramref name="writer"/> through <c>openWrite</c> is owned by this method and
        /// is finalized as soon as the callback completes, so the callback MUST write everything before
        /// returning. <c>openWrite</c> may be called at most once per entry, and an entry whose callback never
        /// called it is written empty. Returning <see langword="false"/> stops after the current entry.
        /// </remarks>
        public async Task<Stream> CompressToArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(outputStream, nameof(outputStream));
            Guard.Against.Null(sources, nameof(sources));
            Guard.Against.Null(writer, nameof(writer));

            var format = archiveFormat ?? DefaultArchiveFormat;

            switch (format)
            {
                case ArchiveFormat.Zip:
                    await WriteZipArchiveAsync(outputStream, sources, writer, cancellationToken);
                    break;
                case ArchiveFormat.Tar:
                    await WriteTarArchiveAsync(outputStream, sources, writer, cancellationToken);
                    break;
                default:
                    throw UnsupportedArchiveFormat(format);
            }

            Rewind(outputStream);
            return outputStream;
        }

        /// <inheritdoc/>
        /// <returns>A pooled, rewound stream holding the archive; the caller must dispose it.</returns>
        public async Task<Stream> CompressToArchiveAsync(IEnumerable<ArchiveEntrySource> sources, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            try
            {
                return await CompressToArchiveAsync(outputStream, sources, archiveFormat, cancellationToken);
            }
            catch
            {
                await outputStream.DisposeAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        /// <returns>A pooled, rewound stream holding the archive; the caller must dispose it.</returns>
        public async Task<Stream> CompressToArchiveAsync(IEnumerable<ArchiveEntrySource> sources, ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            try
            {
                return await CompressToArchiveAsync(outputStream, sources, writer, archiveFormat, cancellationToken);
            }
            catch
            {
                await outputStream.DisposeAsync();
                throw;
            }
        }

        private async Task WriteZipArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, CancellationToken cancellationToken)
        {
            await using var archive = await ZipArchive.CreateAsync(outputStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, cancellationToken);

            foreach (var source in sources)
            {
                Guard.Against.Null(source, nameof(source));
                Guard.Against.NullOrWhiteSpace(source.FullName, nameof(source.FullName));
                Guard.Against.Null(source.OpenReadAsync, nameof(source.OpenReadAsync));

                var zipEntry = archive.CreateEntry(source.FullName, DefaultCompressionLevel);

                // Both scopes end with the loop iteration, in reverse declaration order: the source stream is
                // released first, then the entry stream - and closing the entry stream is what finalizes the
                // ZIP entry, so it must happen before the next CreateEntry call.
                await using var entryStream = await zipEntry.OpenAsync(cancellationToken);
                await using var input = source.OpenReadAsync();

                if (input != null)
                {
                    await CopyAllAsync(input, entryStream, cancellationToken);
                }
            }
        }

        private async Task WriteZipArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, CancellationToken cancellationToken)
        {
            await using var archive = await ZipArchive.CreateAsync(outputStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, cancellationToken);

            var entries = CreateEntryList(0, null, out var archiveInfo);

            var opener = new ZipEntryOpener();
            ICompressionManager.OpenStreamDelegate openWrite = opener.Open;

            foreach (var source in sources)
            {
                Guard.Against.Null(source, nameof(source));
                Guard.Against.NullOrWhiteSpace(source.FullName, nameof(source.FullName));

                var entry = new ArchiveEntry
                {
                    FullName = source.FullName,
                    ArchiveInfo = archiveInfo
                };
                entries.Add(entry);

                // The entry is created before the callback runs so that openWrite is a pure "give me the
                // stream" call; the callback may still rewrite entry.FullName - it just has no effect on
                // an already created ZIP entry, which is why the name is read here.
                opener.Reset(archive.CreateEntry(entry.FullName, DefaultCompressionLevel));

                bool proceed;
                try
                {
                    proceed = await writer(entry, openWrite);
                }
                finally
                {
                    await opener.CloseAsync();
                }

                if (!proceed)
                {
                    return;
                }
            }

            // Measured before the archive is disposed, i.e. without the central directory the ZIP trailer
            // still has to write - see the compression-interface-cleanup task on ArchiveInfo.Size.
            archiveInfo.Size = outputStream.CanSeek ? outputStream.Length : 0L;
        }

        private async Task WriteTarArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, CancellationToken cancellationToken)
        {
            // PAX is the only format of the three that has no path-length or size limits worth worrying about.
            await using var tarWriter = new TarWriter(outputStream, TarEntryFormat.Pax, leaveOpen: true);

            foreach (var source in sources)
            {
                Guard.Against.Null(source, nameof(source));
                Guard.Against.NullOrWhiteSpace(source.FullName, nameof(source.FullName));
                Guard.Against.Null(source.OpenReadAsync, nameof(source.OpenReadAsync));

                // Scoped to the loop iteration: each source stream is released before the next one is opened.
                await using var input = source.OpenReadAsync();

                await WriteTarEntryAsync(tarWriter, source.FullName, input, cancellationToken);
            }
        }

        private async Task WriteTarArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, CancellationToken cancellationToken)
        {
            await using var tarWriter = new TarWriter(outputStream, TarEntryFormat.Pax, leaveOpen: true);

            var entries = CreateEntryList(0, null, out var archiveInfo);

            // TAR needs the entry length in the header BEFORE the data, so the callback writes into a
            // pooled scratch stream that is then attached to the entry.
            var opener = new TempStreamOpener(this);
            ICompressionManager.OpenStreamDelegate openWrite = opener.Open;

            foreach (var source in sources)
            {
                Guard.Against.Null(source, nameof(source));
                Guard.Against.NullOrWhiteSpace(source.FullName, nameof(source.FullName));

                var entry = new ArchiveEntry
                {
                    FullName = source.FullName,
                    ArchiveInfo = archiveInfo
                };
                entries.Add(entry);

                opener.Reset();

                // The name is captured BEFORE the callback runs, exactly like the ZIP path (where the
                // container entry must exist before its stream can be opened). Renaming the entry from
                // inside the callback therefore has no effect in either format - see the
                // compression-interface-cleanup task about IArchiveEntry being mutable.
                var entryName = entry.FullName;

                bool proceed;
                try
                {
                    proceed = await writer(entry, openWrite);
                    entry.Size = await WriteTarEntryAsync(tarWriter, entryName, opener.Stream, cancellationToken);
                }
                finally
                {
                    await opener.CloseAsync();
                }

                if (!proceed)
                {
                    return;
                }
            }

            // Measured before the writer is disposed, i.e. without the two end-of-archive blocks the TAR
            // trailer still has to write - see the compression-interface-cleanup task on ArchiveInfo.Size.
            archiveInfo.Size = outputStream.CanSeek ? outputStream.Length : 0L;
        }

        /// <summary>
        /// Writes one regular-file TAR entry and returns the number of data bytes written. A non-seekable
        /// <paramref name="input"/> is buffered into a pooled stream first, because the TAR header must carry
        /// the exact data length up front.
        /// </summary>
        private async Task<long> WriteTarEntryAsync(TarWriter tarWriter, string name, Stream input, CancellationToken cancellationToken)
        {
            if (input == null)
            {
                // Nothing was produced for this entry: emit it empty rather than skipping it, so the archive
                // still mirrors the requested entry list.
                await tarWriter.WriteEntryAsync(new PaxTarEntry(TarEntryType.RegularFile, name), cancellationToken);
                return 0L;
            }

            if (!input.CanSeek)
            {
                using var seekableInput = await input.ToMemoryAsync(cancellationToken);

                return await WriteTarEntryAsync(tarWriter, name, seekableInput, cancellationToken);
            }

            input.Position = 0L;
            var tarEntry = new PaxTarEntry(TarEntryType.RegularFile, name)
            {
                DataStream = input
            };
            await tarWriter.WriteEntryAsync(tarEntry, cancellationToken);
            return input.Length;
        }

        /// <inheritdoc/>
        public ArchiveFormat GetArchiveFormatByFileExtension(string ext)
        {
            Guard.Against.NullOrWhiteSpace(ext, nameof(ext));

            // Span-based comparison: no ToLower/Trim/Substring allocation on any path.
            var extension = ext.AsSpan().Trim();
            if (extension.Length > 0 && extension[0] == '.')
            {
                extension = extension.Slice(1);
            }

            if (extension.Equals("zip", StringComparison.OrdinalIgnoreCase))
            {
                return ArchiveFormat.Zip;
            }
            if (extension.Equals("7z", StringComparison.OrdinalIgnoreCase))
            {
                return ArchiveFormat.SevenZip;
            }
            if (extension.Equals("rar", StringComparison.OrdinalIgnoreCase))
            {
                return ArchiveFormat.Rar;
            }
            // A compressed tarball is still a TAR container - the outer codec is a CompressionFormat concern.
            if (extension.Equals("tar", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("tgz", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("taz", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("tbz", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("tbz2", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("txz", StringComparison.OrdinalIgnoreCase))
            {
                return ArchiveFormat.Tar;
            }

            throw new NotSupportedException($"File extension '{ext}' does not map to any known {nameof(ArchiveFormat)}");
        }

        /// <summary>
        /// The canonical file extension (including the leading dot) of an archive format.
        /// </summary>
        protected virtual string GetArchiveFileExtension(ArchiveFormat archiveFormat)
        {
            switch (archiveFormat)
            {
                case ArchiveFormat.Zip:
                    return ".zip";
                case ArchiveFormat.SevenZip:
                    return ".7z";
                case ArchiveFormat.Rar:
                    return ".rar";
                case ArchiveFormat.Tar:
                    return ".tar";
                default:
                    throw new NotSupportedException($"Unknown {nameof(ArchiveFormat)}.{archiveFormat}");
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// A missing or foreign extension is APPENDED rather than replaced, so no part of the original name is
        /// lost ("data.bin" -> "data.bin.zip"). An already correct name is returned as-is - same reference, no
        /// allocation. For <see cref="ArchiveFormat.Tar"/> the compressed-tarball shorthands (.tgz, .tbz2, ...)
        /// and the two-part forms (.tar.gz, ...) are accepted as already correct.
        /// </remarks>
        public string FixArchiveFileExtension(string fileName, ArchiveFormat? archiveFormat = null)
        {
            Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));

            var format = archiveFormat ?? DefaultArchiveFormat;
            var expectedExtension = GetArchiveFileExtension(format);

            // Path.GetExtension over a span returns a slice - no string is allocated to make this check.
            var extension = Path.GetExtension(fileName.AsSpan());

            if (extension.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            if (format == ArchiveFormat.Tar && IsTarballExtension(fileName.AsSpan(), extension))
            {
                return fileName;
            }

            return string.Concat(fileName, expectedExtension);
        }

        /// <summary>
        /// True when the name already denotes a (possibly compressed) tarball: either a shorthand extension
        /// like .tgz, or a two-part .tar.&lt;codec&gt; form.
        /// </summary>
        private static bool IsTarballExtension(ReadOnlySpan<char> fileName, ReadOnlySpan<char> extension)
        {
            if (extension.Equals(".tgz", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".taz", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".tbz", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".tbz2", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".txz", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // ".tar.gz" and friends: strip the outer extension and look for ".tar" underneath.
            var innerExtension = Path.GetExtension(fileName.Slice(0, fileName.Length - extension.Length));
            return innerExtension.Equals(".tar", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Opens the current ZIP entry on demand and keeps the single opened stream so the manager can close
        /// it after the callback returned. Reused across all entries of one archive: one object and one
        /// delegate per archive instead of a closure per entry.
        /// </summary>
        private sealed class ZipEntryOpener
        {
            private ZipArchiveEntry _entry;
            private Stream _stream;

            public void Reset(ZipArchiveEntry entry)
            {
                _entry = entry;
                _stream = null;
            }

            public Stream Open()
            {
                if (_entry == null)
                {
                    throw new InvalidOperationException("No archive entry is being processed");
                }
                if (_stream != null)
                {
                    throw new InvalidOperationException("The archive entry stream has already been opened; it may be opened only once per entry");
                }
                _stream = _entry.Open();
                return _stream;
            }

            public async ValueTask CloseAsync()
            {
                var stream = _stream;
                _stream = null;
                _entry = null;
                if (stream != null)
                {
                    // Mandatory for a ZIP being created: the entry is only finalized when its stream closes.
                    await stream.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Hands out the data window of the current TAR entry. The window belongs to the
        /// <see cref="TarReader"/> and stays valid only until the next entry is fetched, so it is NOT disposed
        /// here.
        /// </summary>
        private sealed class TarEntryOpener
        {
            private TarEntry _entry;

            public void Reset(TarEntry entry)
            {
                _entry = entry;
            }

            public Stream Open()
            {
                if (_entry == null)
                {
                    throw new InvalidOperationException("No archive entry is being processed");
                }
                // null for an entry type that carries no data (directory, link, ...).
                return _entry.DataStream;
            }
        }

        /// <summary>
        /// Hands out a pooled scratch stream for the current entry, used where the container needs the entry
        /// length before the data (TAR).
        /// </summary>
        private sealed class TempStreamOpener
        {
            private readonly CompressionManager _owner;
            private Stream _stream;

            public TempStreamOpener(CompressionManager owner)
            {
                _owner = owner;
            }

            public Stream Stream
            {
                get
                {
                    return _stream;
                }
            }

            public void Reset()
            {
                _stream = null;
            }

            public Stream Open()
            {
                if (_stream != null)
                {
                    throw new InvalidOperationException("The archive entry stream has already been opened; it may be opened only once per entry");
                }
                _stream = _owner.CreateTempStream();
                return _stream;
            }

            public async ValueTask CloseAsync()
            {
                var stream = _stream;
                _stream = null;
                if (stream != null)
                {
                    await stream.DisposeAsync();
                }
            }
        }
    }
}
