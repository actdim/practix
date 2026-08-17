using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Serializes objects using their concrete runtime type instead of the declared property type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The declared base type.</typeparam>
    public class RuntimeTypeJsonConverter<T> : JsonConverter<T>
    {
        /// <inheritdoc />
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (T)JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value!.GetType(), options);
        }
    }

    /// <summary>
    /// Serializes arrays of polymorphic objects using the concrete runtime type of each item in the array.
    /// </summary>
    /// <typeparam name="T">The declared array element base type.</typeparam>
    public class RuntimeTypeArrayJsonConverter<T> : JsonConverter<T[]>
    {
        /// <inheritdoc />
        public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T[]>(ref reader, options)!;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, item!.GetType(), options);
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Factory creating runtime-type polymorphic converters for non-sealed types and arrays of non-sealed types.
    /// </summary>
    public class RuntimeTypeConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type type)
        {
            if (type.IsArray)
            {
                var elem = type.GetElementType()!;
                return !elem.IsSealed;
            }

            return !type.IsSealed;
        }

        /// <inheritdoc />
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
