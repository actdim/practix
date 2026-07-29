using System;

namespace THREE.Core.Buffers
{
    public sealed class Float16Array : TypedArray<Half>
    {
        public override string Type => TypedArrays.Float16Array;
    }
}
