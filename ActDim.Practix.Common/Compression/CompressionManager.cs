using System.Reflection;
using System.IO.Compression;
using Ardalis.GuardClauses;
using ActDim.Practix.Extensions;
using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Memory;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
// using Org.BouncyCastle.Utilities.Zlib;

namespace ActDim.Practix.Compression
{
    [Obfuscation(Exclude = true)]
    public class CompressionManager : ICompressionManager
    {
        private const int BufferSize = 80 * 1024; // 80kB

        public CompressionManager()
        {
        }

        protected virtual Stream CreateTempStream()
        {
            // TODO: implement special version of compression manager for large files (use file stream instead of memory stream)
            return MemoryManager.Default.GetStream(nameof(CompressionManager));
        }

        public ArchiveFormat GetArchiveFormat(ReadOnlyMemory<byte> data)
        {
            throw new NotImplementedException();
        }

        public ArchiveFormat GetArchiveFormat(Stream stream)
        {
            throw new NotImplementedException();
        }

        public CompressionFormat GetCompressionFormat(ReadOnlyMemory<byte> data)
        {
            throw new NotImplementedException();
        }

        public CompressionFormat GetCompressionFormat(Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> CompressAsync(ReadOnlyMemory<byte> data, CompressionFormat compressionFormat, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> CompressAsync(Stream stream, CompressionFormat compressionFormat, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DecompressAsync(ReadOnlyMemory<byte> data, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> DecompressAsync(Stream stream, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DecompressAsync(ReadOnlyMemory<byte> data, Stream outputStream, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DecompressAsync(Stream stream, Stream outputStream, CompressionFormat? compressionFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DecompressArchiveAsync(ReadOnlyMemory<byte> data, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DecompressArchiveAsync(Stream stream, ICompressionManager.ArchiveEntryReaderAsyncDelegate reader, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IArchiveEntry>> GetArchiveEntriesAsync(Stream stream, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IArchiveEntry>> GetArchiveEntriesAsync(ReadOnlyMemory<byte> data, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> CompressToArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> CompressToArchiveAsync(Stream outputStream, IEnumerable<ArchiveEntrySource> sources, ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> CompressToArchiveAsync(IEnumerable<ArchiveEntrySource> sources, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> CompressToArchiveAsync(IEnumerable<ArchiveEntrySource> sources, ICompressionManager.ArchiveEntryWriterAsyncDelegate writer, ArchiveFormat? archiveFormat = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ArchiveFormat GetArchiveFormatByFileExtension(string ext)
        {
            throw new NotImplementedException();
        }

        public string FixArchiveFileExtension(string fileName, ArchiveFormat? archiveFormat = null)
        {
            throw new NotImplementedException();
        }

        private static readonly byte[] ZipBytes1 = { 0x50, 0x4b, 0x03, 0x04 };
        private static readonly byte[] ZipBytes2 = { 0x50, 0x4b, 0x07, 0x08 }; // spanned
        private static readonly byte[] GZipBytes = { 0x1f, 0x8b };
        private static readonly byte[] TarBytes = { 0x1f, 0x9d };
        private static readonly byte[] LzhBytes = { 0x1f, 0xa0 };
        private static readonly byte[] BZip2Bytes = { 0x42, 0x5a, 0x68 };
        private static readonly byte[] LZipBytes = { 0x4c, 0x5a, 0x49, 0x50 };

        /*
        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionType"></param>
        /// <returns></returns>
        public bool CheckCompressionType(byte[] data, CompressionType compressionType)
        {
            if (data == null || data.Length == 0)
            {
                return false; // compressionType == CompressionType.None
            }

            // https://en.wikipedia.org/wiki/List_of_file_signatures
            // TODO: implement for all compression types
            switch (compressionType)
            {
                case CompressionType.None:
                    throw new NotSupportedException();
                case CompressionType.GZip:
                    return data.Length >= 2 &&
                        GZipBytes.SequenceEqual(data.Take(2));
                case CompressionType.Zip:
                    // var signature = new Lazy<byte[]>(() => data.Take(4).ToArray());
                    return data.Length >= 4 &&
                        ZipBytes1.SequenceEqual(data.Take(4));
                default:
                    throw new NotImplementedException();
            }
        }

        public bool CheckCompressionType(Stream stream, CompressionType compressionType)
        {
            stream.Seek(0, 0);

            // stream.Position = 0L;

            try
            {
                switch (compressionType)
                {
                    case CompressionType.None:
                        throw new NotSupportedException();
                    case CompressionType.GZip:
                        {
                            var bytes = new byte[2];
                            stream.Read(bytes, 0, 2);
                            return CheckCompressionType(bytes, compressionType);
                        }
                    case CompressionType.Zip:
                        {
                            var bytes = new byte[4];
                            stream.Read(bytes, 0, 4);
                            return CheckCompressionType(bytes, compressionType);
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
            finally
            {
                stream.Seek(0, 0);  // set the stream back to the begining

                // stream.Position = 0L;
            }
        }

        /// <summary>
        /// GetZipArchiveContents
        /// </summary>
        /// <param name="stream">inputStream</param>
        /// <returns></returns>
        public IList<string> GetZipArchiveContents(Stream stream)
        {
            var result = new List<string>();
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                result.AddRange(zipArchive.Entries.Select(entry => entry.FullName));
            }
            return result;
        }

        /// <summary>
        /// GetZipArchiveContents
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public IList<string> GetZipArchiveContents(string fileName)
        {
            var result = new List<string>();
            // input stream
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true))
            {
                using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    result.AddRange(zipArchive.Entries.Select(entry => entry.FullName));
                }
            }
            return result;
        }

        /// <summary>
        /// DecompressZipArchiveAsync
        /// </summary>
        /// <param name="inputStream">inputStream</param>
        /// <param name="callback"></param>
        /// <returns></returns>
		public async Task DecompressZipArchiveAsync(Stream inputStream, Func<ZipArchiveEntry, Task<bool>> callback)
        {
            using (var zipArchive = new ZipArchive(inputStream, ZipArchiveMode.Read))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    if (!await callback(entry))
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async Task DecompressZipArchiveAsync(byte[] data, Func<ZipArchiveEntry, Task<bool>> callback, CancellationToken cancellationToken = default)
        {
            using (var inputStream = CreateTempStream())
            {
                await inputStream.WriteAsync(data, cancellationToken);
                inputStream.Position = 0L;
                using (var zipArchive = new ZipArchive(inputStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        if (!await callback(entry))
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async Task DecompressZipArchiveAsync(ReadOnlyMemory<byte> data, Func<ZipArchiveEntry, Task<bool>> callback, CancellationToken cancellationToken = default)
        {
            using (var inputStream = CreateTempStream())
            {
                await inputStream.WriteAsync(data, cancellationToken);
                inputStream.Position = 0L;
                using (var zipArchive = new ZipArchive(inputStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        if (!await callback(entry))
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// CreateZipArchiveAsync
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="entryName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> CompressToZipArchiveAsync(Stream inputStream,
            string entryName,
            CancellationToken cancellationToken = default)
        {
            using (var outputStream = CreateTempStream())
            {
                using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, false))
                {
                    var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);

                    using (var entryStream = entry.Open())
                    {
                        await inputStream.CopyToAsync(entryStream, BufferSize, cancellationToken);
                    }

                    // outputStream.Seek(0, SeekOrigin.Begin);
                    outputStream.Position = 0L;
                    return outputStream;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> CompressAsync(Stream inputStream, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(inputStream, nameof(inputStream), "Invalid input");
            return await CompressInternalAsync(inputStream.CopyToAsync, compressionType, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task CompressAsync(Stream inputStream, Stream outputStream, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(inputStream, nameof(inputStream), "Invalid input");
            return CompressInternalAsync(inputStream.CopyToAsync, outputStream, compressionType, cancellationToken);
        }

        public async Task<Stream> CompressToZipArchiveAsync(IEnumerable<string> entryNames,
            Func<string, Task<Stream>> reader,
            CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            await CompressToZipArchiveAsync(outputStream, entryNames, reader, cancellationToken);
            return outputStream;
        }

        public async Task CompressToZipArchiveAsync(Stream outputStream,
            IEnumerable<string> entryNames,
            Func<string, Task<Stream>> reader,
            CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(outputStream, nameof(outputStream));
            Guard.Against.Null(entryNames, nameof(entryNames));
            Guard.Against.Null(reader, nameof(reader));
            using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
            {
                foreach (var name in entryNames)
                {
                    var entry = archive.CreateEntry(name);
                    using (var entryStream = entry.Open())
                    {
                        // compression stream
                        using (var bufferedEntryStream = new BufferedStream(entryStream, BufferSize))
                        {
                            var inputStream = await reader(name);
                            await inputStream.CopyToAsync(bufferedEntryStream, cancellationToken);
                        }
                    }
                }
            }
        }

        public async Task<Stream> CompressToZipArchiveAsync(IEnumerable<string> entryNames,
            Func<string, Stream, Task> writer,
            CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            await CompressToZipArchiveAsync(outputStream, entryNames, writer, cancellationToken);
            return outputStream;
        }

        public async Task CompressToZipArchiveAsync(Stream outputStream,
            IEnumerable<string> entryNames,
            Func<string, Stream, Task> writer,
            CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(outputStream, nameof(outputStream));
            Guard.Against.Null(entryNames, nameof(entryNames));
            Guard.Against.Null(writer, nameof(writer));
            using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
            {
                foreach (var name in entryNames)
                {
                    var entry = archive.CreateEntry(name);
                    using (var entryStream = entry.Open())
                    {
                        // compression stream
                        using (var bufferedEntryStream = new BufferedStream(entryStream, BufferSize))
                        {
                            await writer(name, bufferedEntryStream);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> CompressAsync(byte[] data, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(data, nameof(data), "Invalid input");
            return await CompressInternalAsync((s, ct) => s.WriteSafeAsync(data, 8192, ct),
                compressionType, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> CompressAsync(ReadOnlyMemory<byte> data, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            return await CompressInternalAsync((s, ct) => s.WriteAsync(data, ct).AsTask(),
                compressionType, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dataProvider">inputData</param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> CompressInternalAsync(Func<Stream, CancellationToken, Task> dataProvider,
            CompressionType compressionType,
            CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            await CompressInternalAsync(dataProvider, outputStream, compressionType);
            return outputStream;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="outputStream"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        private async Task CompressInternalAsync(Func<Stream, CancellationToken, Task> dataProvider,
            Stream outputStream,
            CompressionType compressionType,
            CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(dataProvider, nameof(dataProvider));
            Guard.Against.Null(dataProvider, nameof(outputStream));

            switch (compressionType)
            {
                case CompressionType.None:
                    throw new ArgumentException(nameof(compressionType));
                case CompressionType.GZip:
                    using (var compressionStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                    {
                        using (var bufferedCompressionStream = new BufferedStream(compressionStream, BufferSize))
                        {
                            await dataProvider(bufferedCompressionStream, cancellationToken);
                            await bufferedCompressionStream.FlushAsync();
                        }
                    }
                    break;
                case CompressionType.Deflate:
                    using (var compressionStream = new DeflateStream(outputStream, CompressionMode.Compress, true))
                    {
                        using (var bufferedCompressionStream = new BufferedStream(compressionStream, BufferSize))
                        {
                            await dataProvider(bufferedCompressionStream, cancellationToken);
                            await bufferedCompressionStream.FlushAsync();
                        }
                    }
                    break;
                case CompressionType.Zip:
                    throw new NotSupportedException($"Can't create ZIP archive with multiple file entries. Use {nameof(CompressToZipArchiveAsync)} method");
                case CompressionType.Brotli:
                    using (var compressionStream = new BrotliStream(outputStream, CompressionMode.Compress, true))
                    {
                        using (var bufferedCompressionStream = new BufferedStream(compressionStream, BufferSize))
                        {
                            await dataProvider(bufferedCompressionStream, cancellationToken);
                            await bufferedCompressionStream.FlushAsync();
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unsupported {nameof(compressionType)}");
            }

            outputStream.Position = 0L;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionType"></param>
        /// <returns></returns>
        public async Task<Stream> DecompressAsync(byte[] data, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            using (var inputStream = CreateTempStream())
            {
                await inputStream.WriteAsync(data, 0, data.Length, cancellationToken);
                inputStream.Position = 0L;
                return await DecompressAsync(inputStream, compressionType);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> DecompressAsync(ReadOnlyMemory<byte> data, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            using (var inputStream = CreateTempStream())
            {
                await inputStream.WriteAsync(data, cancellationToken);
                inputStream.Position = 0L;
                return await DecompressAsync(inputStream, compressionType);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outputStream"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DecompressAsync(byte[] data, Stream outputStream, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            using (var inputStream = CreateTempStream())
            {
                await inputStream.WriteAsync(data, 0, data.Length, cancellationToken);
                inputStream.Position = 0L;
                await DecompressAsync(inputStream, outputStream, compressionType);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outputStream"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DecompressAsync(ReadOnlyMemory<byte> data, Stream outputStream, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            using (var inputStream = CreateTempStream())
            {
                await inputStream.WriteAsync(data, cancellationToken);
                inputStream.Position = 0L;
                await DecompressAsync(inputStream, outputStream, compressionType);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stream">inputStream</param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> DecompressAsync(Stream stream, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            var outputStream = CreateTempStream();
            await DecompressAsync(stream, outputStream, compressionType, cancellationToken);
            return outputStream;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="outputStream"></param>
        /// <param name="compressionType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception
        /// <exception cref="NotSupportedException"></exception>
        public async Task DecompressAsync(Stream stream, Stream outputStream, CompressionType compressionType, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream), "Invalid input");
            Guard.Against.Null(stream, nameof(outputStream), "Invalid input");

            switch (compressionType)
            {
                case CompressionType.None:
                    throw new ArgumentException(nameof(compressionType));
                case CompressionType.GZip:
                    using (var decompressionStream = new GZipStream(stream, CompressionMode.Decompress, true))
                    {
                        using (var bufferedDecompressionStream = new BufferedStream(decompressionStream, BufferSize))
                        {
                            await bufferedDecompressionStream.CopyToAsync(outputStream, cancellationToken);
                        }
                        // await decompressionStream.CopyToAsync(outputStream);
                    }
                    break;
                case CompressionType.Deflate:
                    using (var decompressionStream = new DeflateStream(stream, CompressionMode.Decompress, true))
                    {
                        using (var bufferedDecompressionStream = new BufferedStream(decompressionStream, BufferSize))
                        {
                            await bufferedDecompressionStream.CopyToAsync(outputStream, cancellationToken);
                        }
                    }
                    break;
                case CompressionType.Zip:
                    using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        if (zipArchive.Entries.Count > 1)
                        {
                            throw new NotSupportedException("Can't decompress ZIP archieve (multiple file entries) to one stream");
                        }
                        using (var entryStream = zipArchive.Entries.First().Open())
                        {
                            using (var bufferedDecompressionStream = new BufferedStream(entryStream, BufferSize))
                            {
                                await bufferedDecompressionStream.CopyToAsync(outputStream, cancellationToken);
                            }
                        }
                        // await DecompressZipArchiveAsync(stream, async entry =>
                        // {
                        //     var entryStream = entry.Open();
                        //     await entryStream.CopyToAsync(outputStream, cancellationToken);
                        //     return false;
                        // });
                    }
                    break;
                case CompressionType.Brotli:
                    // https://www.prowaretech.com/articles/current/dot-net/compression-brotli#!
                    using (var decompressionStream = new BrotliStream(stream, CompressionMode.Decompress, true))
                    {
                        using (var bufferedDecompressionStream = new BufferedStream(decompressionStream, BufferSize))
                        {
                            await bufferedDecompressionStream.CopyToAsync(outputStream, cancellationToken);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unsupported {nameof(compressionType)}");
            }

            outputStream.Position = 0L;
        }

        // Byte array output support methods:

        public async Task<byte[]> CompressToBytesAsync(byte[] data, CompressionType compressionType, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await CompressAsync(data, compressionType, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> CompressToBytesAsync(ReadOnlyMemory<byte> data, CompressionType compressionType, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await CompressAsync(data, compressionType, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> CompressToBytesAsync(Stream inputStream, CompressionType compressionType, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await CompressAsync(inputStream, compressionType, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> DecompressToBytesAsync(byte[] data, CompressionType compressionType, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await DecompressAsync(data, compressionType, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> data, CompressionType compressionType, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await DecompressAsync(data, compressionType, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> DecompressToBytesAsync(Stream inputStream, CompressionType compressionType, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await DecompressAsync(inputStream, compressionType, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> CompressToZipArchiveBytesAsync(IEnumerable<string> entryNames, Func<string, Stream, Task> writer, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await CompressToZipArchiveAsync(entryNames, writer, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public async Task<byte[]> CompressToZipArchiveBytesAsync(IEnumerable<string> entryNames, Func<string, Task<Stream>> reader, Func<int, byte[]> dstFactory = null, CancellationToken cancellationToken = default)
        {
            using (var outputStream = await CompressToZipArchiveAsync(entryNames, reader, cancellationToken))
            {
                var outputData = await outputStream.ReadBytesAsync(dstFactory, BufferSize, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
                return outputData;
            }
        }

        public CompressionType GetCompressionTypeByFileExtension(string ext)
        {
            if (".zip".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                return CompressionType.Zip;
            }
            else if (".gz".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                return CompressionType.GZip;
            }
            return CompressionType.None;
        }

        public string FixFileExtension(string name, CompressionType compressionType)
        {
            var ext = Path.GetExtension(name);

            string expectedExt = null;

            switch (compressionType)
            {
                case CompressionType.GZip:
                    expectedExt = ".gz";
                    break;
                case CompressionType.Zip:
                    expectedExt = ".zip";
                    break;
                default:
                    break;
            }

            // !name.EndsWith(expectedExt, StringComparison.OrdinalIgnoreCase)
            if (!string.IsNullOrEmpty(expectedExt) && !string.Equals(ext, expectedExt, StringComparison.OrdinalIgnoreCase))
            {
                return name + expectedExt;
            }

            return name;
        }
        */
    }
}
