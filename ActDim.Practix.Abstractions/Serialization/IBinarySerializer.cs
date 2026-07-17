using System;
using System.Text;

namespace ActDim.Practix.Abstractions.Serialization
{
    /// <summary>Serializes objects to and from their binary (byte array) representation.</summary>
    /// <remarks>The optional <see cref="Encoding"/> controls the byte encoding; when it is
    /// <c>null</c> or UTF-8 the serializer uses the fast UTF-8 path.</remarks>
    public interface IBinarySerializer
    {
        // ── Serialize to bytes ───────────────────────────────────────────────

        byte[] Serialize(object value, Encoding encoding = default);

        byte[] Serialize(object value, Type type, Encoding encoding = default);

        byte[] Serialize<T>(T value, Encoding encoding = default);

        // ── Deserialize from bytes ───────────────────────────────────────────

        object Deserialize(byte[] data, Type type, Encoding encoding = default);

        T Deserialize<T>(byte[] data, Encoding encoding = default);
    }
}
