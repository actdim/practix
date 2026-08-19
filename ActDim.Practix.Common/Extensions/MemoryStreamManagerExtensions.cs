using ActDim.Practix.Common.Introspection;
using ActDim.Practix.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.IO
{
    /// <summary>
    /// Extension methods for <see cref="RecyclableMemoryStreamManager"/> providing caller-tagged context streams.
    /// </summary>
    public static partial class MemoryStreamManagerExtensions
    {
        /// <summary>
        /// Retrieves a recyclable memory stream tagged with the caller method's name for allocation diagnostics.
        /// </summary>
        /// <param name="manager">The recyclable memory stream manager.</param>
        /// <param name="buffer">An optional byte span to initialize the stream content.</param>
        /// <returns>A tagged recyclable <see cref="MemoryStream"/>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MemoryStream GetContextStream(this RecyclableMemoryStreamManager manager, ReadOnlySpan<byte> buffer = default)
        {
            var method = new StackTrace().GetMethod();
            var tag = new MethodIntrospectionInfo(method).Format(IntrospectionFormatType.Normal);
            if (buffer.IsEmpty)
            {
                return manager.GetStream(tag);
            }

            return manager.GetStream(tag, buffer);
        }

        /// <summary>
        /// Retrieves a recyclable memory stream tagged with the caller method's name and pre-populated from a byte array region.
        /// </summary>
        /// <param name="manager">The recyclable memory stream manager.</param>
        /// <param name="buffer">The source byte array.</param>
        /// <param name="offset">The start offset within the buffer.</param>
        /// <param name="count">The number of bytes to copy.</param>
        /// <returns>A tagged recyclable <see cref="MemoryStream"/>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MemoryStream GetContextStream(this RecyclableMemoryStreamManager manager, byte[] buffer, int offset, int count)
        {
            var method = new StackTrace().GetMethod();
            var tag = new MethodIntrospectionInfo(method).Format(IntrospectionFormatType.Normal);
            return manager.GetStream(tag, buffer, offset, count);
        }

        /// <summary>
        /// Retrieves a recyclable memory stream tagged with the caller method's name with a pre-requested capacity.
        /// </summary>
        /// <param name="manager">The recyclable memory stream manager.</param>
        /// <param name="length">The initial required stream capacity in bytes.</param>
        /// <returns>A tagged recyclable <see cref="MemoryStream"/>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MemoryStream GetContextStream(this RecyclableMemoryStreamManager manager, int length)
        {
            var method = new StackTrace().GetMethod();
            var tag = new MethodIntrospectionInfo(method).Format(IntrospectionFormatType.Normal);
            return manager.GetStream(tag, length);
        }
    }
}
