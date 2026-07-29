using System;
using System.Buffers;
using System.Text.Json;

namespace THREE
{
    /// <summary>
    /// Efficient System.Text.Json (de)serialization of a <see cref="SceneDocument"/> to/from a JSON
    /// <see cref="string"/> and UTF-8 bytes. The three.js format itself lives in the document's converter;
    /// this is just fast I/O. Prefer the UTF-8 overloads — STJ works on UTF-8 natively, avoiding the
    /// UTF-16 string round-trip.
    /// </summary>
    public static class SceneDocumentSerializer
    {
        private static readonly JsonSerializerOptions Compact = new();
        private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

        /// <summary>Serializes to a JSON string.</summary>
        public static string ToJson(SceneDocument document, bool indented = false)
        {
            return JsonSerializer.Serialize(document, indented ? Indented : Compact);
        }

        /// <summary>Deserializes from a JSON string.</summary>
        public static SceneDocument FromJson(string json)
        {
            return JsonSerializer.Deserialize<SceneDocument>(json, Compact);
        }

        /// <summary>
        /// Serializes to UTF-8 bytes. Writes through an <see cref="ArrayBufferWriter{T}"/> (a single
        /// growable buffer, no intermediate string), then copies out the exact-length result.
        /// </summary>
        public static byte[] ToBytes(SceneDocument document, bool indented = false)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = indented }))
            {
                JsonSerializer.Serialize(writer, document, Compact);
            }
            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>Deserializes from UTF-8 bytes without decoding to a string.</summary>
        public static SceneDocument FromBytes(ReadOnlyMemory<byte> utf8)
        {
            return JsonSerializer.Deserialize<SceneDocument>(utf8.Span, Compact);
        }
    }
}
