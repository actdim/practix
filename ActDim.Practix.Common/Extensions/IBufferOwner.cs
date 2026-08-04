using System;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public interface IBufferOwner<T> : IDisposable
    {
        T[] Array { get; }

        Memory<T> Memory { get; }

        int Length { get; }
    }
}
