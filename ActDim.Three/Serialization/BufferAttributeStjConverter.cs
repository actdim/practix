using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ActDim.Three.Core;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.Serialization
{
    /// <summary>
    /// System.Text.Json converter: (de)serializes a
    /// <see cref="BufferAttribute"/> in the three.js shape with a typed primitive buffer - no
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
            var arrayPresent = false;
            List<double> array = null;
            List<string> stringArray = null;

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
                    case "name":
                    {
                        name = reader.GetString();
                        break;
                    }
                    case "itemsize":
                    {
                        itemSize = reader.GetInt32();
                        break;
                    }
                    case "count":
                    {
                        count = reader.GetInt32();
                        break;
                    }
                    case "type":
                    {
                        type = reader.GetString();
                        break;
                    }
                    case "normalized":
                    {
                        normalized = reader.GetBoolean();
                        break;
                    }
                    case "dynamic":
                    {
                        dynamic = reader.GetBoolean();
                        break;
                    }
                    case "array":
                    {
                        arrayPresent = true;
                        ReadArray(ref reader, count * itemSize, out array, out stringArray);
                        break;
                    }
                    default:
                    {
                        reader.Skip();
                        break;
                    }
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

            if (stringArray != null)
            {
                attribute.Values = TypedArrays.FromStrings(stringArray);
            }
            else if (array != null)
            {
                attribute.Values = TypedArrays.FromDoubles(type, array);
            }
            else if (arrayPresent)
            {
                // Empty array: pick the buffer kind from the declared type.
                attribute.Values = type == TypedArrays.StringArray
                    ? TypedArrays.FromStrings([])
                    : TypedArrays.FromDoubles(type, []);
            }

            return attribute;
        }

        // Reads a flat JSON array into either a numeric buffer or (custom) a string buffer, decided per
        // element by token type. Mixed arrays are not expected; strings win if any element is a string.
        private static void ReadArray(ref Utf8JsonReader reader, int capacityHint, out List<double> numbers, out List<string> strings)
        {
            // reader is positioned at StartArray.
            numbers = null;
            strings = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.Null)
                {
                    (strings ??= []).Add(reader.GetString());
                }
                else
                {
                    numbers ??= capacityHint > 0 ? new List<double>(capacityHint) : [];
                    numbers.Add(reader.GetDouble());
                }
            }
        }
    }
}
