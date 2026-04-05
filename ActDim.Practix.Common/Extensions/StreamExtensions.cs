using System.Text;
using Ardalis.GuardClauses;
using System.Buffers;
using ActDim.Practix.Memory;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    /// <summary>
    /// Adds overloads to the stream Read method and adds the FullRead method,
    /// which will continue to read until it reads everything that was requested,
    /// or throws an IOException.
    /// </summary>
    public static class StreamExtensions
    {
        private const int BufferSize = 8 * 1024; // 8kB

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ToString(this MemoryStream stream, Encoding encoding)
        {
            Guard.Against.Null(stream, nameof(stream));
            Guard.Against.Null(encoding, nameof(encoding));

            if (stream.Length > int.MaxValue)
            {
                throw new NotSupportedException("Stream is too long");
            }

            stream.Position = 0L;

            if (stream.TryGetBuffer(out ArraySegment<byte> arraySegment))
            {
                // memory stream is exposable
                return encoding.GetString(arraySegment);
                // return encoding.GetString(arraySegment.AsSpan()); // same
                // return encoding.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count); // same
            }
            return encoding.GetString(stream.GetBuffer(), 0, (int)stream.Length);
        }

        /// <summary>
        /// Encode stream bytes to string using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        public static string ToString(this Stream stream, Encoding encoding)
        {
            {
                if (stream is MemoryStream ms)
                {
                    return ms.ToString(encoding);
                }
            }

            Guard.Against.Null(stream, nameof(stream));
            Guard.Against.Null(encoding, nameof(encoding));

            if (stream.CanSeek)
            {
                if (stream.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                stream.Seek(0, SeekOrigin.Begin);

                var length = (int)stream.Length;

                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    // stream.ReadBytes(_ => buffer);
                    stream.Read(buffer, 0, length);
                    return encoding.GetString(buffer, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                // throw new NotSupportedException();
                using (var ms = stream.ToMemory())
                {
                    return ms.ToString(encoding);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToStringAsync(this Stream stream, Encoding encoding, CancellationToken cancellationToken = default)
        {
            {
                if (stream is MemoryStream ms)
                {
                    return ms.ToString(encoding);
                }
            }

            Guard.Against.Null(stream, nameof(stream));
            Guard.Against.Null(encoding, nameof(encoding));

            if (stream.CanSeek)
            {
                if (stream.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                stream.Seek(0, SeekOrigin.Begin);

                var length = (int)stream.Length;

                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    // await stream.ReadBytesAsync(_ => buffer, cancellationToken);
                    await stream.ReadAsync(buffer, 0, length, cancellationToken);
                    return encoding.GetString(buffer, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                // throw new NotSupportedException();
                using (var ms = await stream.ToMemoryAsync(cancellationToken))
                {
                    return await ms.ToStringAsync(encoding, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <param name="bufferSize"></param>
        public static void WriteString(this Stream stream, string str, Encoding encoding = null, int bufferSize = BufferSize)
        {
            Guard.Against.Null(stream, nameof(stream));
            // str.Requires(nameof(str)).IsNotNull();

            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            using (var sw = new StreamWriter(stream, encoding, bufferSize, true))
            {
                sw.Write(str);
                sw.Flush();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <param name="bufferSize"></param>
        public static async void WriteStringAsync(this Stream stream, string str, Encoding encoding = null, int bufferSize = BufferSize)
        {
            Guard.Against.Null(stream, nameof(stream));
            // str.Requires(nameof(str)).IsNotNull();

            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            using (var sw = new StreamWriter(stream, encoding, bufferSize, true))
            {
                await sw.WriteAsync(str);
                await sw.FlushAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static TStream ZeroAllocCopyTo<TStream>(this MemoryStream src, TStream dst) where TStream : Stream
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
                // memory stream is exposable
                dst.Write(arraySegment);
                // dst.Write(arraySegment.AsSpan());
                // dst.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
                return dst;
            }

            // dst.Write(src.GetBuffer().AsSpan(0, (int)src.Length));
            dst.Write(src.GetBuffer(), 0, (int)src.Length);
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
                    return ms.ZeroAllocCopyTo(dst, bufferSize);
                }
            }

            Guard.Against.Null(src, nameof(src));
            Guard.Against.Null(dst, nameof(dst));

            if (src.CanSeek)
            {
                if (src.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                src.Seek(0, SeekOrigin.Begin);

                var length = (int)src.Length;

                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

                try
                {
                    int count;
                    var offset = 0;
                    // src.Read(buffer.AsSpan(0, bufferSize))
                    while ((count = src.Read(buffer, 0, Math.Min(length - offset, bufferSize))) > 0)
                    {
                        offset += count;
                        dst.Write(buffer, 0, count);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                return dst;
            }
            else
            {
                // throw new NotSupportedException();
                using (var ms = src.ToMemory())
                {
                    return ms.ZeroAllocCopyTo(dst, bufferSize);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>        
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<TStream> ZeroAllocCopyToAsync<TStream>(this MemoryStream src, TStream dst, CancellationToken cancellationToken = default) where TStream : Stream
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
                // memory stream is exposable
                await dst.WriteAsync(arraySegment, cancellationToken);
                // await dst.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken);
                return dst;
            }

            // await dst.WriteAsync(src.GetBuffer().AsMemory(0, (int)src.Length), cancellationToken);
            await dst.WriteAsync(src.GetBuffer(), 0, (int)src.Length, cancellationToken);
            return dst;
        }

        /// <summary>
        /// Copy from one stream to another using underlying buffer (if exposable) or using byte array pool
        /// </summary>
        /// <typeparam name="TStream"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<TStream> ZeroAllocCopyToAsync<TStream>(this Stream src, TStream dst, int bufferSize = BufferSize, CancellationToken cancellationToken = default) where TStream : Stream
        {
            {
                if (src is MemoryStream ms)
                {
                    return await ms.ZeroAllocCopyToAsync(dst, bufferSize, cancellationToken);
                }
            }

            Guard.Against.Null(src, nameof(src));
            Guard.Against.Null(dst, nameof(dst));

            if (src.CanSeek)
            {
                if (src.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                src.Seek(0, SeekOrigin.Begin);

                var length = (int)src.Length;

                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

                try
                {
                    int count;
                    var offset = 0;
                    // src.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)
                    while ((count = await src.ReadAsync(buffer, 0, Math.Min(length - offset, bufferSize), cancellationToken)) > 0)
                    {
                        offset += count;
                        // await dst.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                        await dst.WriteAsync(buffer, 0, count, cancellationToken);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                return dst;
            }
            else
            {
                // throw new NotSupportedException();
                using (var ms = await src.ToMemoryAsync())
                {
                    return await ms.ZeroAllocCopyToAsync(dst, bufferSize, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 
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
        /// CloneToMemory/CopyToMemory
        /// </summary>
        /// <param name="src">inputStream</param>
        /// <returns></returns>
        public static async Task<MemoryStream> ToMemoryAsync(this Stream src, CancellationToken cancellationToken = default) // int bufferSize = BufferSize
        {
            Guard.Against.Null(src, nameof(src));
            if (src.CanSeek)
            {
                src.Seek(0, SeekOrigin.Begin);
            }
            // dst
            var outputStream = MemoryManager.Default.GetStream(nameof(StreamExtensions));
            // return await src.ZeroAllocCopyToAsync(outputStream, bufferSize, cancellationToken);            
            await src.CopyToAsync(outputStream, cancellationToken); // bufferSize parameter is ignored in RecyclableMemoryStream implementation of CopyToAsync method
            outputStream.Position = 0L;
            return outputStream;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dstFactory"></param>
        /// <returns></returns>
        public static byte[] ReadBytes(this MemoryStream src, Func<int, byte[]> dstFactory = default)
        {
            src.Position = 0L;

            if (!src.TryGetBuffer(out ArraySegment<byte> arraySegment))
            {
                // throw new InvalidOperationException("Stream is not exposable");
                if (src.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }
                var dst = dstFactory == default ? new byte[src.Length] : dstFactory((int)src.Length);
                Array.Copy(src.GetBuffer(), 0, dst, 0, src.Length);
                return dst;
            }

            return arraySegment.CloneToArray(dstFactory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dstFactory"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static byte[] ReadBytes(this Stream src, Func<int, byte[]> dstFactory = default)
        {
            if (!src.CanSeek)
            {
                // throw new NotSupportedException();
                using (var ms = src.ToMemory())
                {
                    return ms.ReadBytes(dstFactory); ;
                }
            }
            else
            {
                if (src is MemoryStream ms)
                {
                    return ms.ReadBytes(dstFactory);
                }

                Guard.Against.Null(src, nameof(src));

                if (src.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                src.Seek(0, SeekOrigin.Begin);

                var length = src.Length;

                var dst = dstFactory == default ? new byte[length] : dstFactory((int)length);

                // src.Read(dst.AsSpan(0, (int)length));
                src.Read(dst, 0, (int)length);
                return dst;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dstFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<byte[]> ReadBytesAsync(this Stream src, Func<int, byte[]> dstFactory = default, int bufferSize = BufferSize, CancellationToken cancellationToken = default)
        {
            if (!src.CanSeek)
            {
                // throw new NotSupportedException();
                using (var ms = await src.ToMemoryAsync(cancellationToken))
                {
                    return await ms.ReadBytesAsync(dstFactory, bufferSize, cancellationToken);
                }
            }
            else
            {
                if (src is MemoryStream ms)
                {
                    return ms.ReadBytes(dstFactory);
                }

                Guard.Against.Null(src, nameof(src));

                if (src.Length > int.MaxValue)
                {
                    throw new NotSupportedException("Stream is too long");
                }

                src.Seek(0, SeekOrigin.Begin);

                var length = src.Length;

                var dst = dstFactory == default ? new byte[length] : dstFactory((int)length);

                // src.ReadAsync(dst.AsMemory(0, (int)length), cancellationToken);
                await src.ReadAsync(dst, 0, (int)length, cancellationToken);
                return dst;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void WriteBuffered(ReadOnlyMemory<byte> buffer, int bufferSize = BufferSize)
        {
            throw new NotImplementedException();
            // TODO: implement
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Task WriteBufferedAsync(ReadOnlyMemory<byte> buffer, int bufferSize = BufferSize, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
            // TODO: implement
        }

        public static void WriteSafe<TStream>(this TStream dst, byte[] data, int chunkSize = 8192) where TStream : Stream
        {
            for (var i = 0; i < data.Length; i += chunkSize)
            {
                int sizeToWrite = Math.Min(chunkSize, data.Length - i);
                dst.Write(data, i, sizeToWrite);
            }
        }

        public static async Task WriteSafeAsync<TStream>(this TStream dst, byte[] data, int chunkSize = 8192, CancellationToken cancellationToken = default) where TStream : Stream
        {
            for (var i = 0; i < data.Length; i += chunkSize)
            {
                int sizeToWrite = Math.Min(chunkSize, data.Length - i);
                // data.AsMemory(i, sizeToWrite)?
                await dst.WriteAsync(data, i, sizeToWrite, cancellationToken);
            }
        }
    }
}