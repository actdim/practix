
using System;
namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class ArraySegmentExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="dstFactory"></param>
        /// <returns></returns>
		public static T[] CloneToArray<T>(this ArraySegment<T> src, Func<int, T[]> dstFactory = default)
        {
            var count = src.Count;

            var dst = dstFactory == default? new T[count]: dstFactory(count);

            Array.Copy(src.Array, src.Offset, dst, 0, count);

            // need to test it
            // // var sizeOfItem = Marshal.SizeOf(default(T));
            // var sizeOfItem = Marshal.SizeOf<T>();
            // Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset, result, 0, count * sizeOfItem);

            return dst;
        }
    }
}
