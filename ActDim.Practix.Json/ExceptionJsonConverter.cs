using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Serializes <see cref="Exception"/> instances to JSON (type, message, stackTrace, innerException).
    /// Deserialization is not supported.
    /// </summary>
    public class ExceptionJsonConverter : JsonConverter<Exception>
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        /// <inheritdoc />
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing exceptions is not supported.");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.GetType().FullName);
            writer.WriteString("message", value.Message);

            if (value.Source != null)
            {
                writer.WriteString("source", value.Source);
            }

            if (value.HelpLink != null)
            {
                writer.WriteString("helpLink", value.HelpLink);
            }

            if (value.StackTrace != null)
            {
                writer.WriteString("stackTrace", value.StackTrace);
            }

            if (value.InnerException != null)
            {
                writer.WritePropertyName("innerException");
                Write(writer, value.InnerException, options);
            }

            writer.WriteEndObject();
        }
    }
}
