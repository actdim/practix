using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Deserializes object-typed properties to CLR primitives, mimicking Newtonsoft behavior:
    /// JSON number → long or double, string → string, bool → bool,
    /// object → ExpandoObject (supports dynamic access), array → List&lt;object&gt;.
    /// </summary>
    public class ObjectJsonConverter : JsonConverter<object>
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(object);
        }

        /// <inheritdoc />
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadValue(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Null => null,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var l) ? (object)l : reader.GetDouble(),
                JsonTokenType.StartObject => ReadObject(ref reader, options),
                JsonTokenType.StartArray => ReadArray(ref reader, options),
                _ => throw new JsonException($"Unexpected token: {reader.TokenType}")
            };
        }

        private static ExpandoObject ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var expando = new ExpandoObject();
            var dict = (IDictionary<string, object>)expando;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var key = reader.GetString();
                reader.Read();
                dict[key] = ReadValue(ref reader, options);
            }

            return expando;
        }

        private static List<object> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var list = new List<object>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(ReadValue(ref reader, options));
            }

            return list;
        }
    }
}
