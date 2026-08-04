using System.Buffers;
using System;
using System.Threading;
using ActDim.Practix.Extensions;

namespace ActDim.Practix.Common.Memory
{
    public sealed class ArrayPoolBufferOwner<T> : IBufferOwner<T>
    {
        private T[]? _array;
        private readonly ArrayPool<T> _pool;

        public T[] Array =>
            _array ?? throw new ObjectDisposedException(nameof(ArrayPoolBufferOwner<T>));

        public Memory<T> Memory =>
            Array.AsMemory(0, Length);

        public int Length { get; }

        private ArrayPoolBufferOwner(
            T[] array,
            int length,
            ArrayPool<T> pool)
        {
            _array = array;
            Length = length;
            _pool = pool;
        }

        public static ArrayPoolBufferOwner<T> Rent(
            int size,
            ArrayPool<T>? pool = null)
        {
            pool ??= ArrayPool<T>.Shared;

            var array = pool.Rent(size);

            return new ArrayPoolBufferOwner<T>(
                array,
                size,
                pool);
        }

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
