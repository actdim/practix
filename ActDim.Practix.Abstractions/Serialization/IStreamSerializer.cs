using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Serialization
{
    /// <summary>Serializes objects to and from a <see cref="Stream"/>.</summary>
    /// <remarks>The optional <see cref="Encoding"/> controls the byte encoding written to /
    /// read from the stream; when it is <c>null</c> or UTF-8 the serializer uses the fast
    /// UTF-8 path.</remarks>
    public interface IStreamSerializer
    {
        // ── Serialize to stream (sync) ───────────────────────────────────────

        void Serialize(object value, Stream stream, Encoding encoding = default);

        void Serialize(object value, Type type, Stream stream, Encoding encoding = default);

        void Serialize<T>(T value, Stream stream, Encoding encoding = default);

        // ── Serialize to stream (async) ──────────────────────────────────────

        Task SerializeAsync(object value, Stream stream, Encoding encoding = default, CancellationToken cancellationToken = default);

        Task SerializeAsync(object value, Type type, Stream stream, Encoding encoding = default, CancellationToken cancellationToken = default);

        Task SerializeAsync<T>(T value, Stream stream, Encoding encoding = default, CancellationToken cancellationToken = default);

        // ── Deserialize from stream (sync) ───────────────────────────────────

        object Deserialize(Stream stream, Type type, Encoding encoding = default);

        T Deserialize<T>(Stream stream, Encoding encoding = default);

        // ── Deserialize from stream (async) ──────────────────────────────────

        ValueTask<object> DeserializeAsync(Stream stream, Type type, Encoding encoding = default, CancellationToken cancellationToken = default);

        ValueTask<T> DeserializeAsync<T>(Stream stream, Encoding encoding = default, CancellationToken cancellationToken = default);
    }
}
