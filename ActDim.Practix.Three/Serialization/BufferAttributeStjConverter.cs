using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using THREE.Core;
using THREE.Core.Buffers;

namespace THREE.Serialization
{
    /// <summary>
    /// System.Text.Json counterpart of <see cref="BufferAttributeConverter"/>: (de)serializes a
    /// <see cref="BufferAttribute"/> in the three.js shape with a typed primitive buffer — no
    /// <c>object[]</c> and no per-element object boxing.
    /// </summary>
    public sealed class BufferAttributeStjConverter : JsonConverter<BufferAttribute>
    {
        public override void Write(Utf8JsonWriter writer, BufferAttribute attribute, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (attribute.Uuid != Guid.Empty)
            {
                writer.WriteString("uuid", attribute.Uuid.ToString());
            }

            if (!string.IsNullOrEmpty(attribute.Name))
            {
                writer.WriteString("name", attribute.Name);
            }

            writer.WriteNumber("itemSize", attribute.ItemSize);
            writer.WriteNumber("count", attribute.Count);
            writer.WriteString("type", attribute.Type);

            writer.WritePropertyName("array");
            attribute.Values.WriteTo(writer);

            if (attribute.Normalized)
            {
                writer.WriteBoolean("normalized", true);
            }

            writer.WriteEndObject();
        }

        public override BufferAttribute Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string type = null;
            string name = null;
            var uuid = Guid.Empty;
            var itemSize = 0;
            var count = 0;
            var normalized = false;
            var dynamic = false;
            List<double> array = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                var property = reader.GetString();
                reader.Read();

                switch (property.ToLowerInvariant())
                {
                    case "uuid":
                    {
                        var value = reader.GetString();
                        uuid = string.IsNullOrEmpty(value) ? Guid.Empty : Guid.Parse(value);
                        break;
                    }
                    case "name": name = reader.GetString(); break;
                    case "itemsize": itemSize = reader.GetInt32(); break;
                    case "count": count = reader.GetInt32(); break;
                    case "type": type = reader.GetString(); break;
                    case "normalized": normalized = reader.GetBoolean(); break;
                    case "dynamic": dynamic = reader.GetBoolean(); break;
                    case "array": array = ReadNumbers(ref reader, count * itemSize); break;
                    default: reader.Skip(); break;
                }
            }

            var attribute = new BufferAttribute
            {
                Uuid = uuid,
                Name = name,
                ItemSize = itemSize,
                Normalized = normalized,
                Dynamic = dynamic,
            };

            if (array != null)
            {
                attribute.Values = TypedArrays.FromDoubles(type, array);
            }

            return attribute;
        }

        private static List<double> ReadNumbers(ref Utf8JsonReader reader, int capacityHint)
        {
            // reader is positioned at StartArray.
            var list = capacityHint > 0 ? new List<double>(capacityHint) : new List<double>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(reader.GetDouble());
            }
            return list;
        }
    }
}
