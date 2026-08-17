using System;

namespace ActDim.Practix.Common.Memory
{
    /// <summary>
    /// Represents an owned memory buffer wrapper around a backing array slice.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public interface IBufferOwner<T> : IDisposable
    {
        /// <summary>
        /// Gets the underlying backing array.
        /// </summary>
        T[] Array { get; }

        /// <summary>
        /// Gets the active memory region of the buffer.
        /// </summary>
        Memory<T> Memory { get; }

        /// <summary>
        /// Gets the active buffer length.
        /// </summary>
        int Length { get; }
    }
}
