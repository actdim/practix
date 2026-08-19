using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Common.Memory;
using ActDim.Practix.Compression;
using ActDim.Practix.Abstractions.Exceptions;
using Xunit;

// Every operation under test is in-memory and finishes in milliseconds, so threading
// TestContext.Current.CancellationToken through ~100 call sites would only add noise here.
#pragma warning disable xUnit1051

namespace ActDim.Practix.Common.Tests.Compression
{
    public class CompressionManagerTests
    {
        private static readonly CompressionManager Manager = new CompressionManager();

        // Formats the BCL actually ships a codec for.
        public static TheoryData<CompressionFormat> SupportedCompressionFormats =>
            new TheoryData<CompressionFormat>
            {
                CompressionFormat.GZip,
                CompressionFormat.Deflate,
                CompressionFormat.Brotli
            };

        // Formats the enum declares but the BCL cannot handle.
        public static TheoryData<CompressionFormat> UnsupportedCompressionFormats =>
            new TheoryData<CompressionFormat>
            {
                CompressionFormat.BZip2,
                CompressionFormat.LZMA,
                CompressionFormat.LZMA2,
                CompressionFormat.PPMd
            };

        public static TheoryData<ArchiveFormat> SupportedArchiveFormats =>
            new TheoryData<ArchiveFormat>
            {
                ArchiveFormat.Zip,
                ArchiveFormat.Tar
            };

        public static TheoryData<ArchiveFormat> UnsupportedArchiveFormats =>
            new TheoryData<ArchiveFormat>
            {
                ArchiveFormat.SevenZip,
                ArchiveFormat.Rar
            };

        // Highly repetitive payload: compresses well, so ratio assertions are meaningful.
        private static byte[] MakeCompressible(int count)
        {
            var pattern = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. ");
            var data = new byte[count];
            for (var i = 0; i < count; i++)
            {
                data[i] = pattern[i % pattern.Length];
            }

            return data;
        }

        private static byte[] MakeBytes(int count)
        {
            var data = new byte[count];
            for (var i = 0; i < count; i++)
            {
                data[i] = (byte)(i * 31 % 251);
            }

            return data;
        }

        private static async Task<byte[]> ReadAllAsync(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        private static MemoryStream Seekable(byte[] data)
        {
            var ms = new MemoryStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0L;
            return ms;
        }

        // Hides the concrete MemoryStream type and reports CanSeek == false, so the non-seekable branches
        // (buffer-then-sniff, streaming copies) are actually exercised.
        private sealed class NonSeekableStream : Stream
        {
            private readonly MemoryStream _inner;

            public NonSeekableStream()
                : this(Array.Empty<byte>())
            {
            }

            public NonSeekableStream(byte[] data)
            {
                _inner = new MemoryStream();
                _inner.Write(data, 0, data.Length);
                _inner.Position = 0L;
            }

            public byte[] Written
            {
                get
                {
                    return _inner.ToArray();
                }
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                _inner.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }
        }

        private static ArchiveEntrySource Source(string name, byte[] data)
        {
            return new ArchiveEntrySource
            {
                FullName = name,
                OpenReadAsync = () => Seekable(data)
            };
        }

        // Compress / decompress round trips -----------------------------------------------------------------

        [Theory]
        [MemberData(nameof(SupportedCompressionFormats))]
        public async Task Compress_Decompress_Data_RoundTrips(CompressionFormat compressionFormat)
        {
            var data = MakeCompressible(50_000);

            using (var compressed = await Manager.CompressAsync(data, compressionFormat))
            {
                Assert.Equal(0L, compressed.Position);
                Assert.True(compressed.Length < data.Length, "compressible payload should shrink");

                using (var decompressed = await Manager.DecompressAsync(await ReadAllAsync(compressed), compressionFormat))
                {
                    Assert.Equal(0L, decompressed.Position);
                    Assert.Equal(data, await ReadAllAsync(decompressed));
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedCompressionFormats))]
        public async Task Compress_Decompress_Stream_RoundTrips(CompressionFormat compressionFormat)
        {
            var data = MakeBytes(300_000);

            using (var input = Seekable(data))
            using (var compressed = await Manager.CompressAsync(input, compressionFormat))
            using (var output = new MemoryStream())
            {
                await Manager.DecompressAsync(compressed, output, compressionFormat);
                Assert.Equal(data, output.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(SupportedCompressionFormats))]
        public async Task Compress_EmptyPayload_RoundTrips(CompressionFormat compressionFormat)
        {
            using (var compressed = await Manager.CompressAsync(ReadOnlyMemory<byte>.Empty, compressionFormat))
            using (var output = new MemoryStream())
            {
                await Manager.DecompressAsync(compressed, output, compressionFormat);
                Assert.Empty(output.ToArray());
            }
        }

        [Fact]
        public async Task Compress_Stream_ReadsWholeInput_EvenWhenPositionedAtEnd()
        {
            var data = MakeCompressible(1000);

            using (var input = Seekable(data))
            {
                input.Position = input.Length; // the whole stream must still be compressed
                using (var compressed = await Manager.CompressAsync(input, CompressionFormat.GZip))
                {
                    Assert.Equal(data, await Manager.DecompressAsync(compressed, CompressionFormat.GZip));
                }
            }
        }

        [Fact]
        public async Task Compress_ProducesGZipSignature()
        {
            var compressedBytes = await ReadAllAsync(await Manager.CompressAsync(MakeCompressible(100), CompressionFormat.GZip));

            Assert.Equal(0x1F, compressedBytes[0]);
            Assert.Equal(0x8B, compressedBytes[1]);
        }

        [Fact]
        public async Task Compress_IsReadableByBclGZipStream()
        {
            var data = MakeCompressible(20_000);

            using (var compressed = await Manager.CompressAsync(data, CompressionFormat.GZip))
            using (var decoder = new GZipStream(compressed, CompressionMode.Decompress))
            {
                Assert.Equal(data, await ReadAllAsync(decoder));
            }
        }

        [Fact]
        public async Task Decompress_ReadsPayloadProducedByBclGZipStream()
        {
            var data = MakeCompressible(20_000);

            using (var compressed = new MemoryStream())
            {
                using (var encoder = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
                {
                    await encoder.WriteAsync(data);
                }
                compressed.Position = 0L;

                Assert.Equal(data, await Manager.DecompressAsync(compressed));
            }
        }

        [Fact]
        public async Task Decompress_ToByteArray_EmptyPayload_ReturnsEmptyArray()
        {
            using (var compressed = await Manager.CompressAsync(ReadOnlyMemory<byte>.Empty, CompressionFormat.GZip))
            {
                Assert.Empty(await Manager.DecompressAsync(compressed, CompressionFormat.GZip));
            }
        }

        [Fact]
        public async Task Decompress_AutoDetectsGZip()
        {
            var data = MakeCompressible(5_000);

            using (var compressed = await Manager.CompressAsync(data, CompressionFormat.GZip))
            using (var output = new MemoryStream())
            {
                await Manager.DecompressAsync(compressed, output); // no format passed
                Assert.Equal(data, output.ToArray());
            }
        }

        [Fact]
        public async Task Decompress_AutoDetectFromData_Works()
        {
            var data = MakeCompressible(5_000);
            var compressedBytes = await ReadAllAsync(await Manager.CompressAsync(data, CompressionFormat.GZip));

            using (var decompressed = await Manager.DecompressAsync(compressedBytes))
            {
                Assert.Equal(data, await ReadAllAsync(decompressed));
            }
        }

        [Theory]
        [InlineData(CompressionFormat.Brotli)]
        [InlineData(CompressionFormat.Deflate)]
        public async Task Decompress_AutoDetect_HeaderlessFormat_Throws(CompressionFormat compressionFormat)
        {
            var compressedBytes = await ReadAllAsync(await Manager.CompressAsync(MakeCompressible(5_000), compressionFormat));

            // Brotli / raw Deflate carry no signature: detection cannot work, and that must be explicit.
            await Assert.ThrowsAsync<DataFormatException>(() => Manager.DecompressAsync(compressedBytes));
        }

        [Fact]
        public async Task Decompress_NonSeekableInput_ExplicitFormat_Works()
        {
            var data = MakeCompressible(70_000);
            var compressedBytes = await ReadAllAsync(await Manager.CompressAsync(data, CompressionFormat.Brotli));

            using (Stream input = new NonSeekableStream(compressedBytes))
            using (var output = new MemoryStream())
            {
                await Manager.DecompressAsync(input, output, CompressionFormat.Brotli);
                Assert.Equal(data, output.ToArray());
            }
        }

        [Fact]
        public async Task Decompress_NonSeekableInput_AutoDetect_BuffersAndWorks()
        {
            var data = MakeCompressible(70_000);
            var compressedBytes = await ReadAllAsync(await Manager.CompressAsync(data, CompressionFormat.GZip));

            using (Stream input = new NonSeekableStream(compressedBytes))
            using (var output = new MemoryStream())
            {
                await Manager.DecompressAsync(input, output);
                Assert.Equal(data, output.ToArray());
            }
        }

        [Fact]
        public async Task Decompress_NonSeekableOutput_Works()
        {
            var data = MakeCompressible(70_000);

            using (var compressed = await Manager.CompressAsync(data, CompressionFormat.GZip))
            using (var output = new NonSeekableStream())
            {
                await Manager.DecompressAsync(compressed, output, CompressionFormat.GZip);
                Assert.Equal(data, output.Written);
            }
        }

        [Fact]
        public async Task Compress_IntoCallerStream_AppendsAndDoesNotRewind()
        {
            var prefix = Encoding.UTF8.GetBytes("PREFIX");
            var data = MakeCompressible(2_000);

            using (var output = new MemoryStream())
            {
                await output.WriteAsync(prefix);
                await Manager.CompressAsync(data, output, CompressionFormat.GZip);

                // A destination the caller owns is never rewound - composing / appending must stay possible.
                Assert.Equal(output.Length, output.Position);

                var written = output.ToArray();
                Assert.Equal(prefix, written[..prefix.Length]);

                using (var compressed = new MemoryStream(written, prefix.Length, written.Length - prefix.Length))
                {
                    Assert.Equal(data, await Manager.DecompressAsync(compressed, CompressionFormat.GZip));
                }
            }
        }

        [Fact]
        public async Task Decompress_IntoCallerStream_AppendsAtCurrentPosition()
        {
            var prefix = Encoding.UTF8.GetBytes("PREFIX");
            var data = MakeCompressible(2_000);

            using (var compressed = await Manager.CompressAsync(data, CompressionFormat.GZip))
            using (var output = new MemoryStream())
            {
                await output.WriteAsync(prefix);
                await Manager.DecompressAsync(compressed, output, CompressionFormat.GZip);

                Assert.Equal(prefix.Length + data.Length, output.Length);
                Assert.Equal(output.Length, output.Position);
            }
        }

        [Theory]
        [MemberData(nameof(UnsupportedCompressionFormats))]
        public async Task Compress_UnsupportedFormat_Throws(CompressionFormat compressionFormat)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => Manager.CompressAsync(MakeBytes(16), compressionFormat));
        }

        [Theory]
        [MemberData(nameof(UnsupportedCompressionFormats))]
        public async Task Decompress_UnsupportedFormat_Throws(CompressionFormat compressionFormat)
        {
            using (var input = Seekable(MakeBytes(16)))
            using (var output = new MemoryStream())
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => Manager.DecompressAsync(input, output, compressionFormat));
            }
        }

        [Fact]
        public async Task Compress_NullStream_Throws()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Manager.CompressAsync((Stream)null, CompressionFormat.GZip));
        }

        [Fact]
        public async Task Decompress_NullOutputStream_Throws()
        {
            using (var input = Seekable(MakeBytes(16)))
            {
                await Assert.ThrowsAnyAsync<ArgumentException>(() => Manager.DecompressAsync(input, (Stream)null, CompressionFormat.GZip));
            }
        }

        // Pooled byte output (IBufferOwner) -----------------------------------------------------------------

        [Theory]
        [MemberData(nameof(SupportedCompressionFormats))]
        public async Task CompressToBytes_DecompressToBytes_RoundTrips(CompressionFormat compressionFormat)
        {
            var data = MakeCompressible(33_333);

            using (var compressed = await Manager.CompressToBytesAsync(data, compressionFormat))
            {
                Assert.True(compressed.Length < data.Length);

                using (var decompressed = await Manager.DecompressToBytesAsync(compressed.Memory, compressionFormat))
                {
                    // The pooled array may be larger than the payload - Length/Memory carry the valid range.
                    Assert.Equal(data.Length, decompressed.Length);
                    Assert.Equal(data, decompressed.Memory.ToArray());
                }
            }
        }

        [Fact]
        public async Task CompressToBytes_FromStream_RoundTrips()
        {
            var data = MakeBytes(60_000);

            using (var input = Seekable(data))
            using (var compressed = await Manager.CompressToBytesAsync(input, CompressionFormat.Brotli))
            using (var compressedStream = Seekable(compressed.Memory.ToArray()))
            using (var decompressed = await Manager.DecompressToBytesAsync(compressedStream, CompressionFormat.Brotli))
            {
                Assert.Equal(data, decompressed.Memory.ToArray());
            }
        }

        [Fact]
        public async Task DecompressToBytes_AutoDetectsGZip()
        {
            var data = MakeCompressible(4_096);

            using (var compressed = await Manager.CompressToBytesAsync(data, CompressionFormat.GZip))
            using (var decompressed = await Manager.DecompressToBytesAsync(compressed.Memory))
            {
                Assert.Equal(data, decompressed.Memory.ToArray());
            }
        }

        [Fact]
        public async Task CompressToBytes_UsesSuppliedOwnerFactory()
        {
            var requestedLengths = new List<int>();

            using (var compressed = await Manager.CompressToBytesAsync(MakeCompressible(1_000), CompressionFormat.GZip, length =>
            {
                requestedLengths.Add(length);
                return ArrayPoolBufferOwner<byte>.Rent(length);
            }))
            {
                Assert.Equal(compressed.Length, Assert.Single(requestedLengths));
            }
        }

        // Format detection ----------------------------------------------------------------------------------

        [Fact]
        public async Task GetCompressionFormat_DetectsGZip_FromDataAndStream()
        {
            using (var compressed = await Manager.CompressAsync(MakeCompressible(1_000), CompressionFormat.GZip))
            {
                Assert.Equal(CompressionFormat.GZip, Manager.GetCompressionFormat(compressed));
                Assert.Equal(CompressionFormat.GZip, Manager.GetCompressionFormat(await ReadAllAsync(compressed)));
            }
        }

        [Fact]
        public void GetCompressionFormat_DetectsBZip2AndLzma_BySignature()
        {
            // Signature-only formats: recognized, but using them must fail loudly (no BCL codec).
            Assert.Equal(CompressionFormat.BZip2, Manager.GetCompressionFormat(new byte[] { 0x42, 0x5A, 0x68, 0x39, 0x31 }));
            Assert.Equal(CompressionFormat.LZMA, Manager.GetCompressionFormat(new byte[] { 0x5D, 0x00, 0x00, 0x80, 0x00 }));
        }

        [Fact]
        public void GetCompressionFormat_Unknown_Throws()
        {
            Assert.Throws<DataFormatException>(() => Manager.GetCompressionFormat(Encoding.UTF8.GetBytes("plain text, no header")));
        }

        [Fact]
        public void GetCompressionFormat_EmptyData_Throws()
        {
            Assert.Throws<DataFormatException>(() => Manager.GetCompressionFormat(ReadOnlyMemory<byte>.Empty));
        }

        [Fact]
        public async Task GetCompressionFormat_PreservesStreamPosition()
        {
            using (var compressed = await Manager.CompressAsync(MakeCompressible(1_000), CompressionFormat.GZip))
            {
                compressed.Position = 3L;
                Manager.GetCompressionFormat(compressed);
                Assert.Equal(3L, compressed.Position);
            }
        }

        [Fact]
        public void GetCompressionFormat_NonSeekableStream_Throws()
        {
            using (Stream input = new NonSeekableStream(new byte[] { 0x1F, 0x8B, 0x08 }))
            {
                Assert.Throws<NotSupportedException>(() => Manager.GetCompressionFormat(input));
            }
        }

        [Fact]
        public void GetCompressionFormat_NullStream_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => Manager.GetCompressionFormat((Stream)null));
        }

        [Fact]
        public async Task GetArchiveFormat_DetectsZip()
        {
            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", MakeCompressible(100)) }, ArchiveFormat.Zip))
            {
                Assert.Equal(ArchiveFormat.Zip, Manager.GetArchiveFormat(archive));
                Assert.Equal(ArchiveFormat.Zip, Manager.GetArchiveFormat(await ReadAllAsync(archive)));
            }
        }

        [Fact]
        public async Task GetArchiveFormat_DetectsTar_ByUstarMagic()
        {
            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", MakeCompressible(100)) }, ArchiveFormat.Tar))
            {
                Assert.Equal(ArchiveFormat.Tar, Manager.GetArchiveFormat(archive));
            }
        }

        [Fact]
        public void GetArchiveFormat_DetectsSevenZipAndRar_BySignature()
        {
            Assert.Equal(ArchiveFormat.SevenZip, Manager.GetArchiveFormat(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C, 0x00 }));
            Assert.Equal(ArchiveFormat.Rar, Manager.GetArchiveFormat(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 }));
        }

        [Fact]
        public void GetArchiveFormat_Unknown_Throws()
        {
            Assert.Throws<DataFormatException>(() => Manager.GetArchiveFormat(MakeBytes(1_000)));
        }

        // Archive writing / reading -------------------------------------------------------------------------

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_Then_GetArchiveEntries_RoundTrips(ArchiveFormat archiveFormat)
        {
            var first = MakeCompressible(12_345);
            var second = MakeBytes(70_000);
            var sources = new[]
            {
                Source("folder/first.txt", first),
                Source("second.bin", second)
            };

            using (var archive = await Manager.CompressToArchiveAsync(sources, archiveFormat))
            {
                Assert.Equal(0L, archive.Position);

                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);

                Assert.Equal(2, entries.Count);
                Assert.Equal("folder/first.txt", entries[0].FullName);
                Assert.Equal(first.Length, entries[0].Size);
                Assert.Equal("second.bin", entries[1].FullName);
                Assert.Equal(second.Length, entries[1].Size);

                // Every entry points at one shared, fully populated archive descriptor.
                Assert.NotNull(entries[0].ArchiveInfo);
                Assert.Same(entries[0].ArchiveInfo, entries[1].ArchiveInfo);
                Assert.Equal(2, entries[0].ArchiveInfo.Entries.Count);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task DecompressArchive_ReadsEveryEntryContent(ArchiveFormat archiveFormat)
        {
            var payloads = new Dictionary<string, byte[]>
            {
                ["first.txt"] = MakeCompressible(9_999),
                ["nested/second.bin"] = MakeBytes(40_000),
                ["empty.dat"] = Array.Empty<byte>()
            };

            var sources = new List<ArchiveEntrySource>();
            foreach (var pair in payloads)
            {
                sources.Add(Source(pair.Key, pair.Value));
            }

            using (var archive = await Manager.CompressToArchiveAsync(sources, archiveFormat))
            {
                var seen = new Dictionary<string, byte[]>();

                await Manager.DecompressArchiveAsync(archive, async (entry, openRead) =>
                {
                    var entryStream = openRead();
                    seen[entry.FullName] = entryStream == null ? Array.Empty<byte>() : await ReadAllAsync(entryStream);
                    return true;
                }, archiveFormat);

                Assert.Equal(payloads.Count, seen.Count);
                foreach (var pair in payloads)
                {
                    Assert.Equal(pair.Value, seen[pair.Key]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task DecompressArchive_AutoDetectsFormat(ArchiveFormat archiveFormat)
        {
            var data = MakeCompressible(2_048);

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("only.txt", data) }, archiveFormat))
            {
                var count = 0;
                await Manager.DecompressArchiveAsync(archive, async (entry, openRead) =>
                {
                    count++;
                    Assert.Equal(data, await ReadAllAsync(openRead()));
                    return true;
                });

                Assert.Equal(1, count);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task DecompressArchive_ReaderReturningFalse_StopsEnumeration(ArchiveFormat archiveFormat)
        {
            var sources = new[]
            {
                Source("a.txt", MakeCompressible(100)),
                Source("b.txt", MakeCompressible(100)),
                Source("c.txt", MakeCompressible(100))
            };

            using (var archive = await Manager.CompressToArchiveAsync(sources, archiveFormat))
            {
                var visited = 0;
                await Manager.DecompressArchiveAsync(archive, (entry, openRead) =>
                {
                    visited++;
                    return Task.FromResult(false);
                }, archiveFormat);

                Assert.Equal(1, visited);
            }
        }

        [Fact]
        public async Task DecompressArchive_FromData_ReadsEntries()
        {
            var data = MakeCompressible(3_333);
            byte[] archiveBytes;

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", data) }))
            {
                archiveBytes = await ReadAllAsync(archive);
            }

            var seen = 0;
            await Manager.DecompressArchiveAsync(archiveBytes, async (entry, openRead) =>
            {
                seen++;
                Assert.Equal(data, await ReadAllAsync(openRead()));
                return true;
            });

            Assert.Equal(1, seen);
            Assert.Single(await Manager.GetArchiveEntriesAsync(archiveBytes));
        }

        [Fact]
        public async Task DecompressArchive_ZipEntryStream_IsDisposedAfterCallback()
        {
            Stream captured = null;

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", MakeCompressible(100)) }, ArchiveFormat.Zip))
            {
                await Manager.DecompressArchiveAsync(archive, (entry, openRead) =>
                {
                    captured = openRead();
                    return Task.FromResult(false);
                }, ArchiveFormat.Zip);
            }

            Assert.NotNull(captured);
            Assert.ThrowsAny<ObjectDisposedException>(() => captured.ReadByte());
        }

        [Fact]
        public async Task DecompressArchive_OpeningEntryTwice_Throws()
        {
            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", MakeCompressible(100)) }, ArchiveFormat.Zip))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Manager.DecompressArchiveAsync(archive, (entry, openRead) =>
                {
                    openRead();
                    openRead();
                    return Task.FromResult(true);
                }, ArchiveFormat.Zip));
            }
        }

        [Fact]
        public async Task DecompressArchive_NonSeekableTar_StreamsWithoutBuffering()
        {
            var data = MakeBytes(50_000);
            byte[] archiveBytes;

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.bin", data) }, ArchiveFormat.Tar))
            {
                archiveBytes = await ReadAllAsync(archive);
            }

            using (Stream input = new NonSeekableStream(archiveBytes))
            {
                var seen = 0;
                await Manager.DecompressArchiveAsync(input, async (entry, openRead) =>
                {
                    seen++;
                    Assert.Equal(data, await ReadAllAsync(openRead()));
                    return true;
                }, ArchiveFormat.Tar);

                Assert.Equal(1, seen);
            }
        }

        [Fact]
        public async Task DecompressArchive_NonSeekableZip_IsBufferedAndRead()
        {
            var data = MakeCompressible(50_000);
            byte[] archiveBytes;

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", data) }, ArchiveFormat.Zip))
            {
                archiveBytes = await ReadAllAsync(archive);
            }

            using (Stream input = new NonSeekableStream(archiveBytes))
            {
                var seen = 0;
                await Manager.DecompressArchiveAsync(input, async (entry, openRead) =>
                {
                    seen++;
                    Assert.Equal(data, await ReadAllAsync(openRead()));
                    return true;
                });

                Assert.Equal(1, seen);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_NonSeekableSource_IsBuffered(ArchiveFormat archiveFormat)
        {
            var data = MakeBytes(30_000);
            var source = new ArchiveEntrySource
            {
                FullName = "a.bin",
                OpenReadAsync = () => new NonSeekableStream(data)
            };

            using (var archive = await Manager.CompressToArchiveAsync(new[] { source }, archiveFormat))
            {
                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);
                Assert.Equal(data.Length, Assert.Single(entries).Size);
            }
        }

        [Fact]
        public async Task CompressToArchive_IntoCallerStream_ReturnsSameStreamRewound()
        {
            using (var output = new MemoryStream())
            {
                var result = await Manager.CompressToArchiveAsync(output, new[] { Source("a.txt", MakeCompressible(100)) });

                Assert.Same(output, result);
                Assert.Equal(0L, output.Position);
                Assert.True(output.Length > 0);
            }
        }

        [Fact]
        public async Task CompressToArchive_DefaultsToZip()
        {
            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", MakeCompressible(100)) }))
            {
                Assert.Equal(ArchiveFormat.Zip, Manager.GetArchiveFormat(archive));
            }
        }

        [Fact]
        public async Task CompressToArchive_NoSources_ProducesEmptyArchive()
        {
            using (var archive = await Manager.CompressToArchiveAsync(Array.Empty<ArchiveEntrySource>(), ArchiveFormat.Zip))
            {
                Assert.Empty(await Manager.GetArchiveEntriesAsync(archive, ArchiveFormat.Zip));
            }
        }

        [Theory]
        [MemberData(nameof(UnsupportedArchiveFormats))]
        public async Task CompressToArchive_UnsupportedFormat_Throws(ArchiveFormat archiveFormat)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => Manager.CompressToArchiveAsync(Array.Empty<ArchiveEntrySource>(), archiveFormat));
        }

        [Theory]
        [MemberData(nameof(UnsupportedArchiveFormats))]
        public async Task GetArchiveEntries_UnsupportedFormat_Throws(ArchiveFormat archiveFormat)
        {
            using (var input = Seekable(MakeBytes(64)))
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => Manager.GetArchiveEntriesAsync(input, archiveFormat));
            }
        }

        [Fact]
        public async Task CompressToArchive_EntryWithoutName_Throws()
        {
            var sources = new[] { new ArchiveEntrySource { FullName = " ", OpenReadAsync = () => Seekable(MakeBytes(8)) } };

            await Assert.ThrowsAnyAsync<ArgumentException>(() => Manager.CompressToArchiveAsync(sources, ArchiveFormat.Zip));
        }

        [Fact]
        public async Task CompressToArchive_NullSources_Throws()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Manager.CompressToArchiveAsync((IEnumerable<ArchiveEntrySource>)null, ArchiveFormat.Zip));
        }

        [Fact]
        public async Task GetArchiveEntries_ReadsArchiveWrittenByBclZipArchive()
        {
            var data = MakeCompressible(4_096);

            using (var raw = new MemoryStream())
            {
                using (var zip = new ZipArchive(raw, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var entry = zip.CreateEntry("bcl/entry.txt", CompressionLevel.Optimal);
                    using (var entryStream = entry.Open())
                    {
                        await entryStream.WriteAsync(data);
                    }
                }
                raw.Position = 0L;

                var entries = await Manager.GetArchiveEntriesAsync(raw);

                Assert.Equal("bcl/entry.txt", Assert.Single(entries).FullName);
                Assert.Equal(data.Length, entries[0].Size);
            }
        }

        [Fact]
        public async Task CompressToArchive_ProducesArchiveReadableByBclZipArchive()
        {
            var data = MakeCompressible(4_096);

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("entry.txt", data) }, ArchiveFormat.Zip))
            using (var zip = new ZipArchive(archive, ZipArchiveMode.Read))
            {
                var entry = Assert.Single(zip.Entries);
                Assert.Equal("entry.txt", entry.FullName);
                using (var entryStream = entry.Open())
                {
                    Assert.Equal(data, await ReadAllAsync(entryStream));
                }
            }
        }

        [Fact]
        public async Task CompressToArchive_ProducesArchiveReadableByBclTarReader()
        {
            var data = MakeCompressible(4_096);

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("entry.txt", data) }, ArchiveFormat.Tar))
            using (var tarReader = new TarReader(archive))
            {
                var entry = await tarReader.GetNextEntryAsync();
                Assert.NotNull(entry);
                Assert.Equal("entry.txt", entry.Name);
                Assert.Equal(data, await ReadAllAsync(entry.DataStream));
                Assert.Null(await tarReader.GetNextEntryAsync());
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_UnicodeEntryName_RoundTrips(ArchiveFormat archiveFormat)
        {
            const string Name = "папка/файл — тест.txt";
            var data = MakeCompressible(512);

            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source(Name, data) }, archiveFormat))
            {
                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);
                Assert.Equal(Name, Assert.Single(entries).FullName);
            }
        }

        // Entry metadata (type / timestamp / compressed size / link target) ---------------------------------

        // Directories and links cannot be produced through ArchiveEntrySource yet (the write-side gap tracked
        // as P7 in the compression-interface-cleanup task), so these archives are built with the BCL directly -
        // which also makes them a real-world interop check.
        private static async Task<MemoryStream> BuildZipWithDirectoryAsync(byte[] fileData, DateTimeOffset fileTimestamp)
        {
            var raw = new MemoryStream();
            using (var zip = new ZipArchive(raw, ZipArchiveMode.Create, leaveOpen: true))
            {
                zip.CreateEntry("folder/"); // directory, by ZIP convention

                var fileEntry = zip.CreateEntry("folder/file.txt", CompressionLevel.SmallestSize);
                fileEntry.LastWriteTime = fileTimestamp;
                using (var entryStream = fileEntry.Open())
                {
                    await entryStream.WriteAsync(fileData);
                }
            }
            raw.Position = 0L;
            return raw;
        }

        private static async Task<MemoryStream> BuildTarWithDirectoryAndLinkAsync(byte[] fileData, DateTimeOffset fileTimestamp)
        {
            var raw = new MemoryStream();
            await using (var tarWriter = new TarWriter(raw, TarEntryFormat.Pax, leaveOpen: true))
            {
                await tarWriter.WriteEntryAsync(new PaxTarEntry(TarEntryType.Directory, "folder/"));

                using (var data = Seekable(fileData))
                {
                    await tarWriter.WriteEntryAsync(new PaxTarEntry(TarEntryType.RegularFile, "folder/file.txt")
                    {
                        DataStream = data,
                        ModificationTime = fileTimestamp
                    });
                }

                await tarWriter.WriteEntryAsync(new PaxTarEntry(TarEntryType.SymbolicLink, "folder/link.txt")
                {
                    LinkName = "folder/file.txt"
                });
            }
            raw.Position = 0L;
            return raw;
        }

        [Fact]
        public async Task GetArchiveEntries_Zip_ReportsDirectoryTimestampAndCompressedSize()
        {
            var fileData = MakeCompressible(10_000);
            var timestamp = new DateTimeOffset(2021, 3, 4, 5, 6, 8, TimeSpan.Zero); // even seconds: DOS stamps have 2 s resolution

            using (var raw = await BuildZipWithDirectoryAsync(fileData, timestamp))
            {
                var entries = await Manager.GetArchiveEntriesAsync(raw);

                Assert.Equal(2, entries.Count);

                var directory = entries[0];
                Assert.Equal("folder/", directory.FullName);
                Assert.Equal(ArchiveEntryType.Directory, directory.EntryType);
                Assert.Equal(0L, directory.Size);
                Assert.Null(directory.LinkTarget);

                var file = entries[1];
                Assert.Equal(ArchiveEntryType.RegularFile, file.EntryType);
                Assert.Equal(fileData.Length, file.Size);
                // A ZIP timestamp is a timezone-less DOS wall clock, so only the wall-clock value survives -
                // the offset that comes back is the reading machine's local one. Compare accordingly.
                Assert.NotNull(file.LastWriteTime);
                Assert.Equal(timestamp.DateTime, file.LastWriteTime.Value.DateTime);
                // ZIP tracks a per-entry compressed size, and this payload does compress.
                Assert.NotNull(file.CompressedSize);
                Assert.True(file.CompressedSize < file.Size, "compressible entry should be smaller inside the archive");
            }
        }

        [Fact]
        public async Task GetArchiveEntries_Tar_ReportsDirectoryLinkAndTimestamp()
        {
            var fileData = MakeCompressible(10_000);
            var timestamp = new DateTimeOffset(2021, 3, 4, 5, 6, 7, TimeSpan.Zero);

            using (var raw = await BuildTarWithDirectoryAndLinkAsync(fileData, timestamp))
            {
                var entries = await Manager.GetArchiveEntriesAsync(raw, ArchiveFormat.Tar);

                Assert.Equal(3, entries.Count);

                Assert.Equal(ArchiveEntryType.Directory, entries[0].EntryType);
                Assert.Null(entries[0].LinkTarget);

                Assert.Equal(ArchiveEntryType.RegularFile, entries[1].EntryType);
                Assert.Equal(fileData.Length, entries[1].Size);
                Assert.Equal(timestamp, entries[1].LastWriteTime);
                // TAR stores its entries verbatim - there is no per-entry compressed size.
                Assert.Null(entries[1].CompressedSize);

                Assert.Equal(ArchiveEntryType.SymbolicLink, entries[2].EntryType);
                Assert.Equal("folder/file.txt", entries[2].LinkTarget);
            }
        }

        [Fact]
        public async Task DecompressArchive_Zip_DirectoryEntry_IsDistinguishableFromEmptyFile()
        {
            var raw = new MemoryStream();
            using (var zip = new ZipArchive(raw, ZipArchiveMode.Create, leaveOpen: true))
            {
                zip.CreateEntry("folder/");
                zip.CreateEntry("empty.txt");
            }
            raw.Position = 0L;

            var types = new Dictionary<string, ArchiveEntryType>();
            var sizes = new Dictionary<string, long>();

            using (raw)
            {
                await Manager.DecompressArchiveAsync(raw, async (entry, openRead) =>
                {
                    types[entry.FullName] = entry.EntryType;
                    // Both are zero-length; only EntryType tells them apart.
                    sizes[entry.FullName] = (await ReadAllAsync(openRead())).Length;
                    return true;
                });
            }

            Assert.Equal(ArchiveEntryType.Directory, types["folder/"]);
            Assert.Equal(ArchiveEntryType.RegularFile, types["empty.txt"]);
            Assert.Equal(0L, sizes["folder/"]);
            Assert.Equal(0L, sizes["empty.txt"]);
        }

        [Fact]
        public async Task DecompressArchive_Tar_NonFileEntry_HasNoDataStream()
        {
            using (var raw = await BuildTarWithDirectoryAndLinkAsync(MakeCompressible(64), DateTimeOffset.UnixEpoch))
            {
                var streamPresence = new Dictionary<string, bool>();

                await Manager.DecompressArchiveAsync(raw, (entry, openRead) =>
                {
                    streamPresence[entry.FullName] = openRead() != null;
                    return Task.FromResult(true);
                }, ArchiveFormat.Tar);

                Assert.False(streamPresence["folder/"]);
                Assert.True(streamPresence["folder/file.txt"]);
                Assert.False(streamPresence["folder/link.txt"]);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task GetArchiveEntries_OwnWrittenEntries_AreRegularFiles(ArchiveFormat archiveFormat)
        {
            using (var archive = await Manager.CompressToArchiveAsync(new[] { Source("a.txt", MakeCompressible(256)) }, archiveFormat))
            {
                var entry = Assert.Single(await Manager.GetArchiveEntriesAsync(archive, archiveFormat));

                Assert.Equal(ArchiveEntryType.RegularFile, entry.EntryType);
                Assert.Null(entry.LinkTarget);
                Assert.NotNull(entry.LastWriteTime);
            }
        }

        // Archive writing through the writer callback --------------------------------------------------------

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_WithWriter_RoundTrips(ArchiveFormat archiveFormat)
        {
            var payloads = new Dictionary<string, byte[]>
            {
                ["first.txt"] = MakeCompressible(20_000),
                ["second.bin"] = MakeBytes(1_000)
            };

            var sources = new[]
            {
                new ArchiveEntrySource { FullName = "first.txt" },
                new ArchiveEntrySource { FullName = "second.bin" }
            };

            using (var archive = await Manager.CompressToArchiveAsync(sources, async (entry, openWrite) =>
            {
                var entryStream = openWrite();
                await entryStream.WriteAsync(payloads[entry.FullName]);
                return true;
            }, archiveFormat))
            {
                var seen = new Dictionary<string, byte[]>();
                await Manager.DecompressArchiveAsync(archive, async (entry, openRead) =>
                {
                    seen[entry.FullName] = await ReadAllAsync(openRead());
                    return true;
                }, archiveFormat);

                Assert.Equal(payloads.Count, seen.Count);
                foreach (var pair in payloads)
                {
                    Assert.Equal(pair.Value, seen[pair.Key]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_WithWriter_ReportsEntrySize(ArchiveFormat archiveFormat)
        {
            var data = MakeCompressible(7_777);
            IArchiveEntry captured = null;

            using (var archive = await Manager.CompressToArchiveAsync(new[] { new ArchiveEntrySource { FullName = "a.txt" } }, async (entry, openWrite) =>
            {
                captured = entry;
                await openWrite().WriteAsync(data);
                return true;
            }, archiveFormat))
            {
                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);
                Assert.Equal(data.Length, Assert.Single(entries).Size);
            }

            Assert.NotNull(captured);
            Assert.Equal("a.txt", captured.FullName);
            Assert.NotNull(captured.ArchiveInfo);
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_WithWriter_ReturningFalse_StopsAfterCurrentEntry(ArchiveFormat archiveFormat)
        {
            var sources = new[]
            {
                new ArchiveEntrySource { FullName = "a.txt" },
                new ArchiveEntrySource { FullName = "b.txt" }
            };

            var invoked = 0;

            using (var archive = await Manager.CompressToArchiveAsync(sources, async (entry, openWrite) =>
            {
                invoked++;
                await openWrite().WriteAsync(MakeCompressible(64));
                return false;
            }, archiveFormat))
            {
                Assert.Equal(1, invoked);

                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);
                Assert.Equal("a.txt", Assert.Single(entries).FullName);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_WithWriter_EntryNeverOpened_IsWrittenEmpty(ArchiveFormat archiveFormat)
        {
            using (var archive = await Manager.CompressToArchiveAsync(new[] { new ArchiveEntrySource { FullName = "empty.txt" } }, (entry, openWrite) =>
            {
                return Task.FromResult(true);
            }, archiveFormat))
            {
                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);
                Assert.Equal("empty.txt", Assert.Single(entries).FullName);
                Assert.Equal(0L, entries[0].Size);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedArchiveFormats))]
        public async Task CompressToArchive_WithWriter_RenamingEntryInCallback_HasNoEffect(ArchiveFormat archiveFormat)
        {
            // IArchiveEntry is mutable, but the container entry name is fixed before the callback runs in both
            // formats. Documented behaviour, asserted so it stays consistent across formats.
            using (var archive = await Manager.CompressToArchiveAsync(new[] { new ArchiveEntrySource { FullName = "original.txt" } }, async (entry, openWrite) =>
            {
                entry.FullName = "renamed.txt";
                await openWrite().WriteAsync(MakeCompressible(64));
                return true;
            }, archiveFormat))
            {
                var entries = await Manager.GetArchiveEntriesAsync(archive, archiveFormat);
                Assert.Equal("original.txt", Assert.Single(entries).FullName);
            }
        }

        [Fact]
        public async Task CompressToArchive_WithWriter_NullWriter_Throws()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Manager.CompressToArchiveAsync(Array.Empty<ArchiveEntrySource>(), (ICompressionManager.ArchiveEntryWriterAsyncDelegate)null, ArchiveFormat.Zip));
        }

        // File extension helpers ----------------------------------------------------------------------------

        [Theory]
        [InlineData(".zip", ArchiveFormat.Zip)]
        [InlineData("zip", ArchiveFormat.Zip)]
        [InlineData(".ZIP", ArchiveFormat.Zip)]
        [InlineData(".7z", ArchiveFormat.SevenZip)]
        [InlineData(".rar", ArchiveFormat.Rar)]
        [InlineData(".tar", ArchiveFormat.Tar)]
        [InlineData(".tgz", ArchiveFormat.Tar)]
        [InlineData(".tbz2", ArchiveFormat.Tar)]
        [InlineData(".txz", ArchiveFormat.Tar)]
        public void GetArchiveFormatByFileExtension_Maps(string ext, ArchiveFormat expected)
        {
            Assert.Equal(expected, Manager.GetArchiveFormatByFileExtension(ext));
        }

        [Theory]
        [InlineData(".gz")]
        [InlineData(".txt")]
        [InlineData(".")]
        public void GetArchiveFormatByFileExtension_Unknown_Throws(string ext)
        {
            Assert.Throws<NotSupportedException>(() => Manager.GetArchiveFormatByFileExtension(ext));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetArchiveFormatByFileExtension_Blank_Throws(string? ext)
        {
            Assert.ThrowsAny<ArgumentException>(() => Manager.GetArchiveFormatByFileExtension(ext!));
        }

        [Fact]
        public void FixArchiveFileExtension_AlreadyCorrect_ReturnsSameInstance()
        {
            const string FileName = "backup.zip";

            Assert.Same(FileName, Manager.FixArchiveFileExtension(FileName, ArchiveFormat.Zip));
            Assert.Same(FileName, Manager.FixArchiveFileExtension(FileName)); // Zip is the default
        }

        [Theory]
        [InlineData("backup", ArchiveFormat.Zip, "backup.zip")]
        [InlineData("backup.bin", ArchiveFormat.Zip, "backup.bin.zip")]
        [InlineData("backup.BIN", ArchiveFormat.Tar, "backup.BIN.tar")]
        [InlineData("backup", ArchiveFormat.SevenZip, "backup.7z")]
        [InlineData("backup", ArchiveFormat.Rar, "backup.rar")]
        public void FixArchiveFileExtension_Appends(string fileName, ArchiveFormat archiveFormat, string expected)
        {
            Assert.Equal(expected, Manager.FixArchiveFileExtension(fileName, archiveFormat));
        }

        [Theory]
        [InlineData("backup.TAR")]
        [InlineData("backup.tgz")]
        [InlineData("backup.tbz2")]
        [InlineData("backup.tar.gz")]
        [InlineData("backup.tar.bz2")]
        public void FixArchiveFileExtension_Tarball_IsLeftAlone(string fileName)
        {
            Assert.Same(fileName, Manager.FixArchiveFileExtension(fileName, ArchiveFormat.Tar));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void FixArchiveFileExtension_BlankName_Throws(string? fileName)
        {
            Assert.ThrowsAny<ArgumentException>(() => Manager.FixArchiveFileExtension(fileName!, ArchiveFormat.Zip));
        }
    }
}
