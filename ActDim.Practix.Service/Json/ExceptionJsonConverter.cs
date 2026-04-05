using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Json
{
    /// <summary>
    /// Serializes Exception instances to JSON (type, message, stackTrace, innerException).
    /// Deserialization is not supported.
    /// </summary>
    public class ExceptionJsonConverter : JsonConverter<Exception>
    {
        public override bool CanConvert(Type typeToConvert)
            => typeof(Exception).IsAssignableFrom(typeToConvert);

        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Deserializing exceptions is not supported.");

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.GetType().FullName);
            writer.WriteString("message", value.Message);
            if (value.Source != null)
                writer.WriteString("source", value.Source);
            if (value.HelpLink != null)
                writer.WriteString("helpLink", value.HelpLink);
            if (value.StackTrace != null)
                writer.WriteString("stackTrace", value.StackTrace);
            if (value.InnerException != null)
            {
                writer.WritePropertyName("innerException");
                Write(writer, value.InnerException, options);
            }
            writer.WriteEndObject();
        }
    }
}
