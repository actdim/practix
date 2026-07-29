using System;

namespace THREE.Core.Buffers
{
    /// <summary>Base class for a typed numeric buffer backed by a primitive <typeparamref name="T"/> array.</summary>
    public abstract class TypedArray<T> : ITypedArray
    {
        public T[] Data { get; set; }

        public abstract string Type { get; }

        public int Length => Data?.Length ?? 0;

        Array ITypedArray.Data => Data;
    }
}
