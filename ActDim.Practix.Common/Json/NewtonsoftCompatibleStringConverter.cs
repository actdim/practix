using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Common.Json
{
    public class NewtonsoftCompatibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),

                JsonTokenType.Number => reader.GetDecimal().ToString(CultureInfo.InvariantCulture),

                JsonTokenType.True => bool.TrueString,   // "True"
                JsonTokenType.False => bool.FalseString, // "False"

                JsonTokenType.Null => null,

                JsonTokenType.StartObject or JsonTokenType.StartArray =>
                    JsonDocument.ParseValue(ref reader).RootElement.GetRawText(),

                _ => throw new JsonException($"Cannot convert token {reader.TokenType} to string")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
