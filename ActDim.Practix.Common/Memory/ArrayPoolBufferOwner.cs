using System;
using System.Buffers;
using System.Threading;

namespace ActDim.Practix.Common.Memory
{
    /// <summary>
    /// Implements <see cref="IBufferOwner{T}"/> backed by an <see cref="ArrayPool{T}"/> rented array that returns to pool on disposal.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class ArrayPoolBufferOwner<T> : IBufferOwner<T>
    {
        private T[] _array;
        private readonly ArrayPool<T> _pool;

        /// <inheritdoc />
        public T[] Array => _array ?? throw new ObjectDisposedException(nameof(ArrayPoolBufferOwner<T>));

        /// <inheritdoc />
        public Memory<T> Memory => Array.AsMemory(0, Length);

        /// <inheritdoc />
        public int Length { get; }

        private ArrayPoolBufferOwner(T[] array, int length, ArrayPool<T> pool)
        {
            _array = array;
            Length = length;
            _pool = pool;
        }

        /// <summary>
        /// Rents a buffer of at least <paramref name="size"/> elements from the specified <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="size">The required minimum buffer length.</param>
        /// <param name="pool">The array pool to rent from (defaults to <see cref="ArrayPool{T}.Shared"/> when null).</param>
        /// <returns>An owned pooled buffer handle.</returns>
        public static ArrayPoolBufferOwner<T> Rent(int size, ArrayPool<T> pool = null)
        {
            pool ??= ArrayPool<T>.Shared;
            var array = pool.Rent(size);

            return new ArrayPoolBufferOwner<T>(array, size, pool);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var array = Interlocked.Exchange(ref _array, null);
            if (array != null)
            {
                _pool.Return(array);
            }
        }
    }
}
