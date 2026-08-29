using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Three.Serialization;

namespace ActDim.Three
{
    /// <summary>
    /// Efficient System.Text.Json (de)serialization for any three.js object - a whole
    /// <see cref="SceneDocument"/> or an individual <c>Object3D</c>/geometry/material/texture/… String and
    /// UTF-8 byte overloads; prefer the byte overloads (STJ works on UTF-8 natively, no UTF-16 round-trip).
    /// <para>
    /// The options apply the three.js field names (<see cref="DataContractResolver"/>) and the typed-buffer
    /// converter (<see cref="BufferAttributeStjConverter"/>); a <see cref="SceneDocument"/> additionally
    /// routes through its own document converter. Note: serializing a whole node graph directly (instead of
    /// via <see cref="SceneDocument"/>) does not carry child polymorphism - use <c>ToSceneDocument()</c> for scenes.
    /// </para>
    /// </summary>
    public static class ThreeSerializer
    {
        private static readonly JsonSerializerOptions Compact = CreateOptions(indented: false);
        private static readonly JsonSerializerOptions Indented = CreateOptions(indented: true);

        private static JsonSerializerOptions CreateOptions(bool indented)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                TypeInfoResolver = DataContractResolver.Instance,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            };
            options.Converters.Add(new BufferAttributeStjConverter());
            return options;
        }

        /// <summary>Serializes to a JSON string.</summary>
        public static string ToJson<T>(T value, bool indented = false)
        {
            return JsonSerializer.Serialize(value, indented ? Indented : Compact);
        }

        /// <summary>Deserializes from a JSON string.</summary>
        public static T FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Compact);
        }

        /// <summary>
        /// Serializes to UTF-8 bytes through an <see cref="ArrayBufferWriter{T}"/> (a single growable
        /// buffer, no intermediate string), then copies out the exact-length result.
        /// </summary>
        public static byte[] ToBytes<T>(T value, bool indented = false)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = indented }))
            {
                JsonSerializer.Serialize(writer, value, Compact);
            }
            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>
        /// Deserializes from UTF-8 bytes without decoding to a string. Takes a
        /// <see cref="ReadOnlySpan{T}"/> - exactly what STJ consumes; a <c>byte[]</c>,
        /// <c>ReadOnlyMemory&lt;byte&gt;.Span</c>, <c>ArraySegment</c> or <c>stackalloc</c> all fit.
        /// </summary>
        public static T FromBytes<T>(ReadOnlySpan<byte> utf8)
        {
            return JsonSerializer.Deserialize<T>(utf8, Compact);
        }

        /// <summary>Serializes as UTF-8 to a stream.</summary>
        public static void ToStream<T>(Stream stream, T value, bool indented = false)
        {
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
            {
                JsonSerializer.Serialize(writer, value, Compact);
            }
        }

        /// <summary>Deserializes from a UTF-8 stream.</summary>
        public static T FromStream<T>(Stream stream)
        {
            return JsonSerializer.Deserialize<T>(stream, Compact);
        }

        /// <summary>Asynchronously serializes as UTF-8 to a stream (STJ streams in chunks - no full buffering).</summary>
        public static Task ToStreamAsync<T>(Stream stream, T value, bool indented = false, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.SerializeAsync(stream, value, indented ? Indented : Compact, cancellationToken);
        }

        /// <summary>Asynchronously deserializes from a UTF-8 stream.</summary>
        public static ValueTask<T> FromStreamAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, Compact, cancellationToken);
        }
    }
}
