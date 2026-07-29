using System;

namespace THREE.Core.Buffers
{
    /// <summary>
    /// A homogeneous numeric buffer that mirrors a JS TypedArray. The concrete implementation carries a
    /// primitive <c>T[]</c> backing (no per-element boxing) and reports the three.js <see cref="Type"/>
    /// string explicitly (the CLR element type is not enough — e.g. both <see cref="Uint8Array"/> and
    /// <see cref="Uint8ClampedArray"/> are backed by <c>byte[]</c>). Serializer-agnostic: writing is done
    /// by each converter over the concrete type.
    /// </summary>
    public interface ITypedArray
    {
        /// <summary>three.js TypedArray discriminator, e.g. "Float32Array".</summary>
        string Type { get; }

        /// <summary>Number of elements in the backing array.</summary>
        int Length { get; }

        /// <summary>The primitive backing array (for inspection / equality).</summary>
        Array Data { get; }
    }
}
