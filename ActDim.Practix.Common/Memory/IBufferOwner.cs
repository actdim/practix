using System;

namespace ActDim.Practix.Common.Memory
{
    public interface IBufferOwner<T> : IDisposable
    {
        T[] Array { get; }

        Memory<T> Memory { get; }

        int Length { get; }
    }
}
