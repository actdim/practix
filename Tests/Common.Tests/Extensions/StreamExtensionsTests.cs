using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.Extensions;
using ActDim.Practix.Common.Memory;
using Xunit;

namespace ActDim.Practix.Common.Tests.Extensions
{
    public class StreamExtensionsTests
    {
        private const string SampleText = "Hello, 世界! 😀 — mixed ASCII + Japanese + emoji (surrogate pair).";

        private static byte[] MakeBytes(int count)
        {
            var data = new byte[count];
            for (var i = 0; i < count; i++)
            {
                data[i] = (byte)(i % 256);
            }

            return data;
        }

        // Exposable MemoryStream (default constructors expose their buffer via TryGetBuffer).
        private static MemoryStream Exposable(byte[] data)
        {
            var ms = new MemoryStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0L;
            return ms;
        }

        // Non-exposable MemoryStream: TryGetBuffer returns false and GetBuffer throws - exercises the
        // pooled-read fallback path.
        private static MemoryStream NonExposable(byte[] data)
        {
            return new MemoryStream(data, 0, data.Length, writable: false, publiclyVisible: false);
        }

        // Stream wrapper that hides the concrete MemoryStream type (so `is MemoryStream` is false) and can
        // toggle CanSeek, letting us exercise the generic seekable and non-seekable branches.
        private sealed class WrapperStream : Stream
        {
            private readonly MemoryStream _inner;
            private readonly bool _canSeek;

            public WrapperStream(byte[] data, bool canSeek)
            {
                _inner = new MemoryStream();
                _inner.Write(data, 0, data.Length);
                _inner.Position = 0L;
                _canSeek = canSeek;
            }

            public override bool CanRead => true;

            public override bool CanSeek => _canSeek;

            public override bool CanWrite => true;

            public override long Length => _canSeek ? _inner.Length : throw new NotSupportedException();

            public override long Position
            {
                get => _canSeek ? _inner.Position : throw new NotSupportedException();
                set
                {
                    if (!_canSeek)
                    {
                        throw new NotSupportedException();
                    }

                    _inner.Position = value;
                }
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
                if (!_canSeek)
                {
                    throw new NotSupportedException();
                }

                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }
        }

        // ToString ---------------------------------------------------------------------------------------

        [Fact]
        public void GetString_MemoryStream_Exposable_ReturnsText()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using var ms = Exposable(bytes);

            Assert.Equal(SampleText, ms.GetString(Encoding.UTF8));
        }

        [Fact]
        public void GetString_MemoryStream_NonExposable_ReturnsText()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using var ms = NonExposable(bytes);

            Assert.Equal(SampleText, ms.GetString(Encoding.UTF8));
        }

        [Fact]
        public void GetString_Stream_SeekableNonMemory_ReturnsText()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using Stream src = new WrapperStream(bytes, canSeek: true);

            Assert.Equal(SampleText, src.GetString(Encoding.UTF8));
        }

        [Fact]
        public void GetString_Stream_NonSeekable_ReturnsText()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using Stream src = new WrapperStream(bytes, canSeek: false);

            Assert.Equal(SampleText, src.GetString(Encoding.UTF8));
        }

        [Fact]
        public void GetString_UnicodeEncoding_RoundTrips()
        {
            var bytes = Encoding.Unicode.GetBytes(SampleText);
            using var ms = Exposable(bytes);

            Assert.Equal(SampleText, ms.GetString(Encoding.Unicode));
        }

        [Fact]
        public void GetString_EmptyStream_ReturnsEmpty()
        {
            using var ms = Exposable(Array.Empty<byte>());

            Assert.Equal(string.Empty, ms.GetString(Encoding.UTF8));
        }

        [Fact]
        public void GetString_NullStream_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => ((Stream)null).GetString(Encoding.UTF8));
        }

        [Fact]
        public void GetString_NoEncoding_DefaultsToUtf8()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using var ms = Exposable(bytes);

            Assert.Equal(SampleText, ms.GetString());          // encoding omitted -> UTF-8
            ms.Position = 0L;
            Assert.Equal(SampleText, ms.GetString(null));      // explicit null -> UTF-8
        }

        [Fact]
        public async Task GetStringAsync_Seekable_ReturnsText()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using Stream src = new WrapperStream(bytes, canSeek: true);

            Assert.Equal(SampleText, await src.GetStringAsync(Encoding.UTF8, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task GetStringAsync_NonSeekable_ReturnsText()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            using Stream src = new WrapperStream(bytes, canSeek: false);

            Assert.Equal(SampleText, await src.GetStringAsync(Encoding.UTF8, TestContext.Current.CancellationToken));
        }

        // WriteString ------------------------------------------------------------------------------------

        [Fact]
        public void WriteString_WritesEncodedBytes_WithoutBom()
        {
            using var ms = new MemoryStream();

            ms.WriteString("abc");

            Assert.Equal(new byte[] { 0x61, 0x62, 0x63 }, ms.ToArray());
        }

        [Fact]
        public void WriteString_RoundTripsWithToString()
        {
            using var ms = new MemoryStream();

            ms.WriteString(SampleText);
            ms.Position = 0L;

            Assert.Equal(SampleText, ms.GetString(Encoding.UTF8));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WriteString_NullOrEmpty_WritesNothing(string? str)
        {
            using var ms = new MemoryStream();

            ms.WriteString(str);

            Assert.Equal(0, ms.Length);
        }

        [Fact]
        public void WriteString_NullStream_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => ((Stream)null).WriteString("x"));
        }

        [Fact]
        public async Task WriteStringAsync_RoundTripsWithToString()
        {
            using var ms = new MemoryStream();

            await ms.WriteStringAsync(SampleText, ct: TestContext.Current.CancellationToken);
            ms.Position = 0L;

            Assert.Equal(SampleText, await ms.GetStringAsync(Encoding.UTF8, TestContext.Current.CancellationToken));
        }

        // ZeroAllocCopyTo --------------------------------------------------------------------------------

        [Fact]
        public void ZeroAllocCopyTo_MemoryStream_Exposable_CopiesAll()
        {
            var data = MakeBytes(1234);
            using var src = Exposable(data);
            using var dst = new MemoryStream();

            var result = src.ZeroAllocCopyTo(dst);

            Assert.Same(dst, result);
            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public void ZeroAllocCopyTo_MemoryStream_NonExposable_CopiesAll()
        {
            var data = MakeBytes(1234);
            using var src = NonExposable(data);
            using var dst = new MemoryStream();

            src.ZeroAllocCopyTo(dst);

            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public void ZeroAllocCopyTo_Stream_Seekable_LargerThanBuffer_CopiesAll()
        {
            var data = MakeBytes(20_000); // > 8 KB internal buffer -> multiple read iterations
            using Stream src = new WrapperStream(data, canSeek: true);
            using var dst = new MemoryStream();

            src.ZeroAllocCopyTo(dst);

            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public void ZeroAllocCopyTo_Stream_NonSeekable_CopiesAll()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: false);
            using var dst = new MemoryStream();

            src.ZeroAllocCopyTo(dst);

            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public async Task ZeroAllocCopyToAsync_MemoryStream_Exposable_CopiesAll()
        {
            var data = MakeBytes(1234);
            using var src = Exposable(data);
            using var dst = new MemoryStream();

            var result = await src.ZeroAllocCopyToAsync(dst, ct: TestContext.Current.CancellationToken);

            Assert.Same(dst, result);
            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public async Task ZeroAllocCopyToAsync_Stream_Seekable_LargerThanBuffer_CopiesAll()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: true);
            using var dst = new MemoryStream();

            await src.ZeroAllocCopyToAsync(dst, ct: TestContext.Current.CancellationToken);

            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public async Task ZeroAllocCopyToAsync_Stream_NonSeekable_CopiesAll()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: false);
            using var dst = new MemoryStream();

            await src.ZeroAllocCopyToAsync(dst, ct: TestContext.Current.CancellationToken);

            Assert.Equal(data, dst.ToArray());
        }

        // ToMemory ---------------------------------------------------------------------------------------

        // Note: ToMemory returns a RecyclableMemoryStream whose manager forbids ToArray(), so we read the
        // content back via ReadExactly instead of ToArray().
        private static byte[] DrainFromStart(Stream stream, int length)
        {
            stream.Position = 0L;
            var buffer = new byte[length];
            stream.ReadExactly(buffer, 0, length);
            return buffer;
        }

        [Fact]
        public void ToMemory_ReturnsSeekableStreamAtZero_WithSameContent()
        {
            var data = MakeBytes(5000);
            using Stream src = new WrapperStream(data, canSeek: true);

            using var mem = src.ToMemory();

            Assert.True(mem.CanSeek);
            Assert.Equal(0L, mem.Position);
            Assert.Equal(data.Length, mem.Length);
            Assert.Equal(data, DrainFromStart(mem, data.Length));
        }

        [Fact]
        public void ToMemory_NonSeekableSource_Works()
        {
            var data = MakeBytes(5000);
            using Stream src = new WrapperStream(data, canSeek: false);

            using var mem = src.ToMemory();

            Assert.Equal(data.Length, mem.Length);
            Assert.Equal(data, DrainFromStart(mem, data.Length));
        }

        [Fact]
        public async Task ToMemoryAsync_ReturnsSeekableStreamAtZero_WithSameContent()
        {
            var data = MakeBytes(5000);
            using Stream src = new WrapperStream(data, canSeek: true);

            using var mem = await src.ToMemoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0L, mem.Position);
            Assert.Equal(data.Length, mem.Length);
            Assert.Equal(data, DrainFromStart(mem, data.Length));
        }

        // ReadBytes --------------------------------------------------------------------------------------

        [Fact]
        public void ReadBytes_MemoryStream_Exposable_ReturnsContentAndExactLength()
        {
            var data = MakeBytes(1000);
            using var src = Exposable(data);

            using var owner = src.ReadBytes();

            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        [Fact]
        public void ReadBytes_MemoryStream_NonExposable_ReturnsContent()
        {
            var data = MakeBytes(1000);
            using var src = NonExposable(data);

            using var owner = src.ReadBytes();

            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        [Fact]
        public void ReadBytes_Stream_Seekable_ReturnsContent()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: true);

            using var owner = src.ReadBytes();

            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        [Fact]
        public void ReadBytes_Stream_NonSeekable_ReturnsContent()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: false);

            using var owner = src.ReadBytes();

            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        [Fact]
        public void ReadBytes_CustomOwnerFactory_IsUsed()
        {
            var data = MakeBytes(64);
            using var src = Exposable(data);
            var factoryCalledWith = -1;

            using var owner = src.ReadBytes(length =>
            {
                factoryCalledWith = length;
                return new ArrayBufferOwner<byte>(new byte[length]);
            });

            Assert.Equal(data.Length, factoryCalledWith);
            Assert.IsType<ArrayBufferOwner<byte>>(owner);
            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        [Fact]
        public async Task ReadBytesAsync_Seekable_ReturnsContent()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: true);

            using var owner = await src.ReadBytesAsync(ct: TestContext.Current.CancellationToken);

            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        [Fact]
        public async Task ReadBytesAsync_NonSeekable_ReturnsContent()
        {
            var data = MakeBytes(20_000);
            using Stream src = new WrapperStream(data, canSeek: false);

            using var owner = await src.ReadBytesAsync(ct: TestContext.Current.CancellationToken);

            Assert.Equal(data.Length, owner.Length);
            Assert.Equal(data, owner.Memory.ToArray());
        }

        // WriteInChunks --------------------------------------------------------------------------------------

        [Fact]
        public void WriteInChunks_WritesAllBytes_WithSmallChunks()
        {
            var data = MakeBytes(1000);
            using var dst = new MemoryStream();

            dst.WriteInChunks(data, chunkSize: 64); // chunk smaller than data -> multiple iterations

            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public async Task WriteInChunksAsync_WritesAllBytes_WithSmallChunks()
        {
            var data = MakeBytes(1000);
            using var dst = new MemoryStream();

            await dst.WriteInChunksAsync(data, 64, TestContext.Current.CancellationToken);

            Assert.Equal(data, dst.ToArray());
        }

        [Fact]
        public void WriteInChunks_NullDst_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => ((Stream)null).WriteInChunks(MakeBytes(4)));
        }

        [Fact]
        public void WriteInChunks_NullData_Throws()
        {
            using var dst = new MemoryStream();

            Assert.ThrowsAny<ArgumentException>(() => dst.WriteInChunks(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WriteInChunks_NonPositiveChunkSize_Throws(int chunkSize)
        {
            using var dst = new MemoryStream();

            Assert.ThrowsAny<ArgumentException>(() => dst.WriteInChunks(MakeBytes(4), chunkSize));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task WriteInChunksAsync_NonPositiveChunkSize_Throws(int chunkSize)
        {
            using var dst = new MemoryStream();

            await Assert.ThrowsAnyAsync<ArgumentException>(() => dst.WriteInChunksAsync(MakeBytes(4), chunkSize, TestContext.Current.CancellationToken));
        }
    }
}
