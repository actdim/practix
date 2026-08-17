using System;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ArraySegment{T}"/>.
    /// </summary>
    public static class ArraySegmentExtensions
    {
        /// <summary>
        /// Creates a new array containing a copy of the elements in the specified <see cref="ArraySegment{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the segment.</typeparam>
        /// <param name="src">The source array segment.</param>
        /// <param name="dstFactory">An optional custom array factory delegate used to allocate the destination array.</param>
        /// <returns>A new array populated with the copied elements.</returns>
        public static T[] CloneToArray<T>(this ArraySegment<T> src, Func<long, T[]> dstFactory = default)
        {
            var count = src.Count;
            var dst = dstFactory == default ? new T[count] : dstFactory(count);

            if (src.Array != null && count > 0)
            {
                Array.Copy(src.Array, src.Offset, dst, 0, count);
            }

            return dst;
        }
    }
}
