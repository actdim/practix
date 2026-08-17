using System;

namespace ActDim.Practix.Common.Memory
{
    /// <summary>
    /// Implements <see cref="IBufferOwner{T}"/> backed by a plain non-pooled managed heap array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class ArrayBufferOwner<T> : IBufferOwner<T>
    {
        /// <inheritdoc />
        public T[] Array { get; }

        /// <inheritdoc />
        public Memory<T> Memory => Array.AsMemory(0, Length);

        /// <inheritdoc />
        public int Length { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBufferOwner{T}"/> class wrapping the specified array.
        /// </summary>
        /// <param name="array">The backing array.</param>
        /// <param name="length">The active length slice (defaults to full array length if negative).</param>
        public ArrayBufferOwner(T[] array, int length = -1)
        {
            Array = array ?? throw new ArgumentNullException(nameof(array));
            Length = length < 0 ? array.Length : length;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
