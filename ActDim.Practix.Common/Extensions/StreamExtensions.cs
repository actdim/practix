using System.Text;
using Ardalis.GuardClauses;
using System.Buffers;
using ActDim.Practix.Memory;
using ActDim.Practix.Common.Memory;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Adds overloads to the stream Read method and adds the FullRead method,
    /// which will continue to read until it reads everything that was requested,
    /// or throws an IOException.
    /// </summary>
    public static class StreamExtensions
    {
        internal const int BufferSize = 8 * 1024; // 8kB

        // Default text encoding: UTF-8 WITHOUT a BOM. Encoding.UTF8 emits a preamble (EF BB BF), which
        // StreamWriter writes on the first write to a stream at position 0. A BOM is almost never wanted
        // and breaks concatenation, hashing/signatures, byte comparisons and many parsers - so it is opt-in
        // via an explicitly passed encoding, never the default.
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static void PooledCopy(Stream src, Stream dst, int bufferSize)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int count;
                while ((count = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    dst.Write(buffer, 0, count);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task PooledCopyAsync(Stream src, Stream dst, int bufferSize, CancellationToken ct)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int count;
                while ((count = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, count), ct);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Decode the whole memory stream into a string. Uses the exposed underlying buffer when available
        /// (no copy) and falls back to a pooled buffer for a non-exposable stream. The only allocation is the
        /// resulting string.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding">Text encoding; defaults to UTF-8 without a BOM when null.</param>
        /// <returns></returns>
        public static string GetString(this MemoryStream stream, Encoding encoding = null)
        {
            Guard.Against.Null(stream, nameof(stream));
            encoding ??= Utf8NoBom;

            if (stream.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            stream.Position = 0L;

            if (stream.TryGetBuffer(out ArraySegment<byte> arraySegment))
            {
                // Memory stream is exposable: decode straight from the underlying buffer, no copy.
                // Unlike Stream.Write, Encoding.GetString has no ArrayPool fallback - the span and
                // (array, offset, count) overloads both pin the memory and share the same core, so
                // the only allocation is the resulting string.
                return encoding.GetString(arraySegment);
            }

            // non-exposable MemoryStream: GetBuffer() raises UnauthorizedAccessException
            var length = checked((int)stream.Length);
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                stream.ReadExactly(buffer, 0, length);
                return encoding.GetString(buffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Encode stream bytes to string using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        public static string GetString(this Stream stream, Encoding encoding = null)
        {
            {
                if (stream is MemoryStream ms)
                {
                    return ms.GetString(encoding);
                }
            }

            Guard.Against.Null(stream, nameof(stream));
            encoding ??= Utf8NoBom;

            if (stream.CanSeek)
            {
                if (stream.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                stream.Seek(0, SeekOrigin.Begin);

                var length = checked((int)stream.Length);

                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {

                    stream.ReadExactly(buffer, 0, length);
                    return encoding.GetString(buffer, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                using var ms = stream.ToMemory();
                return ms.GetString(encoding);
            }
        }

        /// <summary>
        /// Decode the whole stream into a string. For a <see cref="MemoryStream"/> uses the exposed buffer;
        /// for any other seekable stream reads into a pooled buffer; non-seekable streams are first buffered
        /// into memory. The only allocation is the resulting string.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding">Text encoding; defaults to UTF-8 without a BOM when null.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> GetStringAsync(this Stream stream, Encoding encoding = null, CancellationToken ct = default)
        {
            {
                if (stream is MemoryStream ms)
                {
                    return ms.GetString(encoding);
                }
            }

            Guard.Against.Null(stream, nameof(stream));
            encoding ??= Utf8NoBom;

            if (stream.CanSeek)
            {
                if (stream.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                stream.Seek(0, SeekOrigin.Begin);

                var length = checked((int)stream.Length);

                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    await stream.ReadExactlyAsync(buffer, 0, length, ct);
                    return encoding.GetString(buffer, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                using var ms = await stream.ToMemoryAsync(ct);
                return await ms.GetStringAsync(encoding, ct);
            }
        }

        /// <summary>
        /// Encode a string and write it to the stream without allocating a <see cref="StreamWriter"/>:
        /// the bytes are produced into a pooled buffer via <see cref="Encoding.GetBytes(string)"/>'s span
        /// overload. Defaults to UTF-8 without a BOM (see <see cref="Utf8NoBom"/>).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="str"></param>
        /// <param name="encoding">Text encoding; defaults to UTF-8 without a BOM when null.</param>
        /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
        public static int WriteString(this Stream stream, string str, Encoding encoding = null)
        {
            Guard.Against.Null(stream, nameof(stream));

            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            encoding ??= Utf8NoBom;

            // Single-shot on purpose (encode the whole string into one pooled buffer) rather than a
            // chunked loop. Chunking would need a stateful Encoder to carry surrogate/shift state across
            // buffer boundaries (a naive slice-and-GetBytes corrupts split surrogate pairs), and
            // encoding.GetEncoder() allocates one Encoder per call - so chunking ADDS a managed allocation
            // to the hot path, defeating the zero-alloc goal. Single-shot keeps the common case (small /
            // medium strings) allocation-free: one pooled rent, returned in finally.
            // Trade-off: for a huge string GetByteCount can exceed the shared pool's max bucket (~1 MB),
            // and Rent then returns a plain heap array. That is acceptable here since these helpers target
            // ordinary strings; bounding peak memory for multi-MB inputs is what a chunked Encoder path is for.
            var buffer = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(str));
            try
            {
                var written = encoding.GetBytes(str, buffer);
                stream.Write(buffer, 0, written);
                return written;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Encode a string and write it to the stream without allocating a <see cref="StreamWriter"/>:
        /// the bytes are produced into a pooled buffer via <see cref="Encoding.GetBytes(string)"/>'s span
        /// overload. Defaults to UTF-8 without a BOM (see <see cref="Utf8NoBom"/>).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="str"></param>
        /// <param name="encoding">Text encoding; defaults to UTF-8 without a BOM when null.</param>
        /// <param name="ct"></param>
        /// <returns>Number of bytes written to <paramref name="stream"/>.</returns>
        public static async Task<int> WriteStringAsync(this Stream stream, string str, Encoding encoding = null, CancellationToken ct = default)
        {
            Guard.Against.Null(stream, nameof(stream));

            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            encoding ??= Utf8NoBom;

            // Single-shot into one pooled buffer - see WriteString for why this beats a chunked Encoder loop
            // (chunking would allocate an Encoder per call and defeat the zero-alloc goal).
            var buffer = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(str));
            try
            {
                var written = encoding.GetBytes(str, buffer);
                await stream.WriteAsync(buffer.AsMemory(0, written), ct);
                return written;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Copy the whole memory stream into <paramref name="dst"/>. Writes directly from the exposed
        /// underlying buffer when available (no intermediate copy) and falls back to a pooled buffer for a
        /// non-exposable stream. Returns <paramref name="dst"/> for chaining.
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="bufferSize">Pooled buffer size used only for the non-exposable fallback; the exposable
        /// fast path writes the underlying buffer in a single call and ignores it.</param>
        /// <returns></returns>
        public static TStream ZeroAllocCopyTo<TStream>(this MemoryStream src, TStream dst, int bufferSize = BufferSize) where TStream : Stream
        {
            Guard.Against.Null(src, nameof(src));
            Guard.Against.Null(dst, nameof(dst));

            if (src.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            src.Position = 0L;

            if (src.TryGetBuffer(out ArraySegment<byte> arraySegment))
            {
                // Memory stream is exposable.
                // Prefer the (array, offset, count) overload over Write(ReadOnlySpan<byte>) / AsSpan():
                // every Stream overrides Write(byte[], int, int) and writes directly, whereas the base
                // Write(ReadOnlySpan<byte>) fallback (used by streams that do NOT override it) first rents
                // a buffer from ArrayPool and copies the span into it. The array overload keeps this fast
                // path allocation-free for any dst, and for streams that do override the span overload it
                // is exactly equivalent. bufferSize is irrelevant here - the whole buffer is written at once.
                dst.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
                return dst;
            }

            // non-exposable MemoryStream: GetBuffer() raises UnauthorizedAccessException, copy via pool
            PooledCopy(src, dst, bufferSize);
            return dst;
        }

        /// <summary>
        /// Copy from one stream to another using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        /// <typeparam name = "TStream" ></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static TStream ZeroAllocCopyTo<TStream>(this Stream src, TStream dst, int bufferSize = BufferSize) where TStream : Stream
        {
            {
                if (src is MemoryStream ms)
                {
                    // Binds to the more specific MemoryStream overload (fast path).
                    return ms.ZeroAllocCopyTo(dst, bufferSize);
                }
            }

            Guard.Against.Null(src, nameof(src));
            Guard.Against.Null(dst, nameof(dst));

            if (src.CanSeek)
            {
                src.Seek(0, SeekOrigin.Begin);
                PooledCopy(src, dst, bufferSize);
                return dst;
            }
            else
            {
                using var ms = src.ToMemory();
                return ms.ZeroAllocCopyTo(dst, bufferSize);
            }

        }

        /// <summary>
        /// Copy the whole memory stream into <paramref name="dst"/>. Writes directly from the exposed
        /// underlying buffer when available (no intermediate copy) and falls back to a pooled buffer for a
        /// non-exposable stream. Returns <paramref name="dst"/> for chaining.
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="bufferSize">Pooled buffer size used only for the non-exposable fallback; the exposable
        /// fast path writes the underlying buffer in a single call and ignores it.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<TStream> ZeroAllocCopyToAsync<TStream>(this MemoryStream src, TStream dst, int bufferSize = BufferSize, CancellationToken ct = default) where TStream : Stream
        {
            Guard.Against.Null(src, nameof(src));
            Guard.Against.Null(dst, nameof(dst));

            if (src.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            src.Position = 0L;

            if (src.TryGetBuffer(out ArraySegment<byte> arraySegment))
            {
                // Memory stream is exposable.
                // Unlike the sync path, keep the ReadOnlyMemory<byte> overload here: for an array-backed
                // segment it is already allocation-free (its base impl recovers the array via
                // MemoryMarshal.TryGetArray), so it does NOT suffer the ArrayPool copy that the sync
                // Write(ReadOnlySpan<byte>) fallback does. It is also the BCL-recommended overload (CA1835).
                // bufferSize is irrelevant here - the whole buffer is written at once.
                await dst.WriteAsync(arraySegment, ct);
                return dst;
            }

            // non-exposable MemoryStream: GetBuffer() raises UnauthorizedAccessException, copy via pool
            await PooledCopyAsync(src, dst, bufferSize, ct);
            return dst;
        }

        /// <summary>
        /// Copy from one stream to another using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="bufferSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<TStream> ZeroAllocCopyToAsync<TStream>(this Stream src, TStream dst, int bufferSize = BufferSize, CancellationToken ct = default) where TStream : Stream
        {
            {
                if (src is MemoryStream ms)
                {
                    // Binds to the more specific MemoryStream overload (fast path).
                    return await ms.ZeroAllocCopyToAsync(dst, bufferSize, ct);
                }
            }

            Guard.Against.Null(src, nameof(src));
            Guard.Against.Null(dst, nameof(dst));

            if (src.CanSeek)
            {
                src.Seek(0, SeekOrigin.Begin);
                await PooledCopyAsync(src, dst, bufferSize, ct);
                return dst;
            }
            else
            {
                using var ms = await src.ToMemoryAsync(ct);
                return await ms.ZeroAllocCopyToAsync(dst, bufferSize, ct);
            }
        }

        /// <summary>
        /// Copy the whole stream into a seekable in-memory stream (a pooled RecyclableMemoryStream) positioned
        /// at 0. The caller owns the returned stream and MUST dispose it. Copying uses ArrayPool internally
        /// (Stream.CopyTo), so no large intermediate array is allocated on the heap.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static MemoryStream ToMemory(this Stream src) // int bufferSize = BufferSize
        {
            Guard.Against.Null(src, nameof(src));
            if (src.CanSeek)
            {
                src.Seek(0, SeekOrigin.Begin);
            }
            // dst
            var outputStream = MemoryManager.Default.GetStream(nameof(StreamExtensions));
            // return src.ZeroAllocCopyTo(outputStream, bufferSize);
            src.CopyTo(outputStream); // bufferSize parameter is ignored in RecyclableMemoryStream implementation of CopyTo method
            outputStream.Position = 0L;
            return outputStream;
        }

        /// <summary>
        /// Copy the whole stream into a seekable in-memory stream (a pooled RecyclableMemoryStream) positioned
        /// at 0. The caller owns the returned stream and MUST dispose it. Copying uses ArrayPool internally
        /// (Stream.CopyToAsync), so no large intermediate array is allocated on the heap.
        /// </summary>
        /// <param name="src">inputStream</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<MemoryStream> ToMemoryAsync(this Stream src, CancellationToken ct = default) // int bufferSize = BufferSize
        {
            Guard.Against.Null(src, nameof(src));
            if (src.CanSeek)
            {
                src.Seek(0, SeekOrigin.Begin);
            }
            // dst
            var outputStream = MemoryManager.Default.GetStream(nameof(StreamExtensions));
            // return await src.ZeroAllocCopyToAsync(outputStream, bufferSize, ct);
            await src.CopyToAsync(outputStream, ct); // bufferSize parameter is ignored in RecyclableMemoryStream implementation of CopyToAsync method
            outputStream.Position = 0L;
            return outputStream;
        }

        // Create the destination buffer owner: either from the caller-supplied factory (e.g. a pooled or
        // preallocated owner) or, by default, a pooled ArrayPoolBufferOwner. The owner carries its valid
        // Length and returns the buffer to the pool on Dispose - callers must dispose it (via using).
        // Contract: a custom factory MUST return an owner whose Length equals the requested length (the
        // backing array may be larger); otherwise the owner's Length/Memory would misreport the valid range.
        private static IBufferOwner<byte> RentOwner(Func<int, IBufferOwner<byte>> ownerFactory, int length)
        {
            return ownerFactory?.Invoke(length) ?? ArrayPoolBufferOwner<byte>.Rent(length);
        }

        /// <summary>
        /// Read the whole stream into a buffer owned by an <see cref="IBufferOwner{T}"/>. The owner exposes
        /// the valid length (<see cref="IBufferOwner{T}.Length"/> / <see cref="IBufferOwner{T}.Memory"/>) and
        /// returns the buffer to the pool on <see cref="IDisposable.Dispose"/>, so the caller MUST dispose it.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="ownerFactory">Factory that creates the destination buffer owner for the requested length;
        /// defaults to a pooled <see cref="ArrayPoolBufferOwner{T}"/>. It MUST return an owner whose
        /// <see cref="IBufferOwner{T}.Length"/> equals the requested length (its backing array may be larger);
        /// otherwise the returned owner would report a wrong valid length.</param>
        /// <returns></returns>
        public static IBufferOwner<byte> ReadBytes(this MemoryStream src, Func<int, IBufferOwner<byte>> ownerFactory = null)
        {
            Guard.Against.Null(src, nameof(src));

            src.Position = 0L;

            if (src.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            var length = (int)src.Length;
            var owner = RentOwner(ownerFactory, length);

            if (src.TryGetBuffer(out ArraySegment<byte> arraySegment))
            {
                // Memory stream is exposable: copy straight from the underlying buffer into the owner.
                // We copy on purpose (rather than aliasing the stream's buffer) so the owner stays valid
                // after the stream is disposed or reused - critical for RecyclableMemoryStream.
                arraySegment.AsSpan().CopyTo(owner.Memory.Span);
            }
            else
            {
                // non-exposable MemoryStream: GetBuffer() raises UnauthorizedAccessException, read instead
                src.ReadExactly(owner.Array, 0, length);
            }

            return owner;
        }

        /// <summary>
        /// Read the whole stream into a buffer owned by an <see cref="IBufferOwner{T}"/>. The owner exposes
        /// the valid length and returns the buffer to the pool on <see cref="IDisposable.Dispose"/>, so the
        /// caller MUST dispose it.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="ownerFactory">Factory that creates the destination buffer owner for the requested length;
        /// defaults to a pooled <see cref="ArrayPoolBufferOwner{T}"/>. It MUST return an owner whose
        /// <see cref="IBufferOwner{T}.Length"/> equals the requested length (its backing array may be larger);
        /// otherwise the returned owner would report a wrong valid length.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static IBufferOwner<byte> ReadBytes(this Stream src, Func<int, IBufferOwner<byte>> ownerFactory = null)
        {
            Guard.Against.Null(src, nameof(src));

            if (!src.CanSeek)
            {
                using var ms = src.ToMemory();
                return ms.ReadBytes(ownerFactory);
            }

            if (src is MemoryStream memoryStream)
            {
                return memoryStream.ReadBytes(ownerFactory);
            }

            if (src.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            src.Seek(0, SeekOrigin.Begin);

            var length = (int)src.Length;
            var owner = RentOwner(ownerFactory, length);
            src.ReadExactly(owner.Array, 0, length);
            return owner;
        }

        /// <summary>
        /// Read the whole stream into a buffer owned by an <see cref="IBufferOwner{T}"/>. The owner exposes
        /// the valid length and returns the buffer to the pool on <see cref="IDisposable.Dispose"/>, so the
        /// caller MUST dispose it.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="ownerFactory">Factory that creates the destination buffer owner for the requested length;
        /// defaults to a pooled <see cref="ArrayPoolBufferOwner{T}"/>. It MUST return an owner whose
        /// <see cref="IBufferOwner{T}.Length"/> equals the requested length (its backing array may be larger);
        /// otherwise the returned owner would report a wrong valid length.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static async Task<IBufferOwner<byte>> ReadBytesAsync(this Stream src, Func<int, IBufferOwner<byte>> ownerFactory = null, CancellationToken ct = default)
        {
            Guard.Against.Null(src, nameof(src));

            if (!src.CanSeek)
            {
                using var ms = await src.ToMemoryAsync(ct);
                return ms.ReadBytes(ownerFactory);
            }

            if (src is MemoryStream memoryStream)
            {
                return memoryStream.ReadBytes(ownerFactory);
            }

            if (src.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            src.Seek(0, SeekOrigin.Begin);

            var length = (int)src.Length;
            var owner = RentOwner(ownerFactory, length);
            await src.ReadExactlyAsync(owner.Array, 0, length, ct);
            return owner;
        }

        /// <summary>
        /// Write <paramref name="data"/> to <paramref name="dst"/> in bounded chunks of at most
        /// <paramref name="chunkSize"/> bytes, so no single Write call receives an oversized buffer
        /// (some stream implementations behave badly with very large single writes).
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="dst"></param>
        /// <param name="data"></param>
        /// <param name="chunkSize">Maximum bytes per <see cref="Stream.Write(byte[], int, int)"/> call.</param>
        /// <remarks>
        /// Not needed for a plain <see cref="FileStream"/> or <see cref="MemoryStream"/>, where a single
        /// <c>Write(data, 0, data.Length)</c> is equivalent.
        /// </remarks>
        public static void WriteInChunks<TStream>(this TStream dst, byte[] data, int chunkSize = BufferSize) where TStream : Stream
        {
            Guard.Against.Null(dst, nameof(dst));
            Guard.Against.Null(data, nameof(data));
            Guard.Against.NegativeOrZero(chunkSize, nameof(chunkSize));

            for (var i = 0; i < data.Length; i += chunkSize)
            {
                int sizeToWrite = Math.Min(chunkSize, data.Length - i);
                dst.Write(data, i, sizeToWrite);
            }
        }

        /// <summary>
        /// Write <paramref name="data"/> to <paramref name="dst"/> in bounded chunks of at most
        /// <paramref name="chunkSize"/> bytes, so no single Write call receives an oversized buffer
        /// (some stream implementations behave badly with very large single writes).
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="dst"></param>
        /// <param name="data"></param>
        /// <param name="chunkSize">Maximum bytes per write call, and therefore the cancellation granularity.</param>
        /// <param name="ct"></param>
        /// <remarks>
        /// Beyond bounding the buffer size, chunking gives <paramref name="ct"/> somewhere to be
        /// observed: a single <see cref="Stream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>
        /// over a large buffer may not notice cancellation until it has finished.
        /// </remarks>
        public static async Task WriteInChunksAsync<TStream>(this TStream dst, byte[] data, int chunkSize = BufferSize, CancellationToken ct = default) where TStream : Stream
        {
            Guard.Against.Null(dst, nameof(dst));
            Guard.Against.Null(data, nameof(data));
            Guard.Against.NegativeOrZero(chunkSize, nameof(chunkSize));

            for (var i = 0; i < data.Length; i += chunkSize)
            {
                int sizeToWrite = Math.Min(chunkSize, data.Length - i);
                await dst.WriteAsync(data.AsMemory(i, sizeToWrite), ct);
            }
        }
    }
}
