using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Common.Json
{
    public class RuntimeTypeJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => (T)JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, value!.GetType(), options);
    }

    public class RuntimeTypeArrayJsonConverter<T> : JsonConverter<T[]>
    {
        public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<T[]>(ref reader, options)!;

        public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
                JsonSerializer.Serialize(writer, item, item!.GetType(), options);
            writer.WriteEndArray();
        }
    }

    public class RuntimeTypeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type)
        {
            if (type.IsArray)
            {
                var elem = type.GetElementType()!;
                return !elem.IsSealed;
            }
            return !type.IsSealed;
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            if (type.IsArray)
            {
                var elem = type.GetElementType()!;
                return (JsonConverter)Activator.CreateInstance(
                    typeof(RuntimeTypeArrayJsonConverter<>).MakeGenericType(elem))!;
            }
            return (JsonConverter)Activator.CreateInstance(
                typeof(RuntimeTypeJsonConverter<>).MakeGenericType(type))!;
        }
    }
}
