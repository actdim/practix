using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Json
{
    public class RuntimeTypeJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => (T)JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, value!.GetType(), options);
    }

    public class RuntimeTypeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type) => !type.IsSealed;

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            var converterType = typeof(RuntimeTypeJsonConverter<>).MakeGenericType(type);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
