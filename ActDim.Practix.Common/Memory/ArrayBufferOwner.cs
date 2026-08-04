using ActDim.Practix.Extensions;
using System;

namespace ActDim.Practix.Common.Memory
{
    public sealed class ArrayBufferOwner<T> : IBufferOwner<T>
    {
        public T[] Array { get; }

        public Memory<T> Memory =>
            Array.AsMemory(0, Length);

        public int Length { get; }

        public ArrayBufferOwner(
            T[] array,
            int length = -1)
        {
            Array = array;
            Length = length < 0 ? array.Length : length;
        }

        public void Dispose()
        {
        }
    }
}
