using ActDim.Practix.Introspection;
using Microsoft.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static partial class MemoryStreamManagerExtensions
    {
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MemoryStream GetContextStream(this RecyclableMemoryStreamManager manager, byte[] buffer, int offset, int count)
        {
            var method = new StackTrace().GetMethod();
            var tag = new MethodIntrospectionInfo(method).Format(IntrospectionFormatType.Normal);
            return manager.GetStream(tag, buffer, offset, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MemoryStream GetContextStream(this RecyclableMemoryStreamManager manager, int length)
        {
            var method = new StackTrace().GetMethod();
            var tag = new MethodIntrospectionInfo(method).Format(IntrospectionFormatType.Normal);
            return manager.GetStream(tag, length);
        }
    }

}
