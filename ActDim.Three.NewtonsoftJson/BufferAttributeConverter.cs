using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using ActDim.Three.Core;
using ActDim.Three.Core.Buffers;

namespace ActDim.Three.NewtonsoftJson
{
    /// <summary>
    /// (De)serializes a <see cref="BufferAttribute"/> in the three.js shape
    /// (<c>{ uuid, name, itemSize, count, type, array, normalized }</c>) using Newtonsoft.Json.
    /// </summary>
    public class BufferAttributeConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => objectType == typeof(BufferAttribute);

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var attr = (BufferAttribute)value;

            writer.WriteStartObject();

            if (attr.Uuid != Guid.Empty)
            {
                writer.WritePropertyName("uuid");
                writer.WriteValue(attr.Uuid.ToString());
            }

            if (!string.IsNullOrEmpty(attr.Name))
            {
                writer.WritePropertyName("name");
                writer.WriteValue(attr.Name);
            }

            writer.WritePropertyName("itemSize");
            writer.WriteValue(attr.ItemSize);

            writer.WritePropertyName("count");
            writer.WriteValue(attr.Count);

            // `type` is written before `array` so a reader can stream straight into the typed buffer.
            writer.WritePropertyName("type");
            writer.WriteValue(attr.Type);

            writer.WritePropertyName("array");
            attr.Values.WriteTo(writer);

            if (attr.Normalized)
            {
                writer.WritePropertyName("normalized");
                writer.WriteValue(true);
            }

            writer.WriteEndObject();
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

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

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }

                var prop = ((string)reader.Value).ToLowerInvariant();
                switch (prop)
                {
                    case "uuid":
                    {
                        var s = reader.ReadAsString();
                        uuid = string.IsNullOrEmpty(s) ? Guid.Empty : Guid.Parse(s);
                        break;
                    }
                    case "name":
                    {
                        name = reader.ReadAsString();
                        break;
                    }
                    case "itemsize":
                    {
                        itemSize = reader.ReadAsInt32() ?? 0;
                        break;
                    }
                    case "count":
                    {
                        count = reader.ReadAsInt32() ?? 0;
                        break;
                    }
                    case "type":
                    {
                        type = reader.ReadAsString();
                        break;
                    }
                    case "normalized":
                    {
                        normalized = reader.ReadAsBoolean() ?? false;
                        break;
                    }
                    case "dynamic":
                    {
                        dynamic = reader.ReadAsBoolean() ?? false;
                        break;
                    }
                    case "array":
                    {
                        arrayPresent = true;
                        ReadArray(reader, count * itemSize, out array, out stringArray);
                        break;
                    }
                    default:
                    {
                        reader.Read();
                        reader.Skip();
                        break;
                    }
                }
            }

            var attr = new BufferAttribute
            {
                Uuid = uuid, // preserved from JSON; never regenerated
                Name = name,
                ItemSize = itemSize,
                Normalized = normalized,
                Dynamic = dynamic,
            };

            if (stringArray != null)
            {
                attr.Values = TypedArrays.FromStrings(stringArray);
            }
            else if (array != null)
            {
                attr.Values = TypedArrays.FromDoubles(type, array);
            }
            else if (arrayPresent)
            {
                attr.Values = type == TypedArrays.StringArray
                    ? TypedArrays.FromStrings([])
                    : TypedArrays.FromDoubles(type, []);
            }

            return attr;
        }

        private static void ReadArray(JsonReader reader, int capacityHint, out List<double> numbers, out List<string> strings)
        {
            numbers = null;
            strings = null;

            reader.Read(); // advance to StartArray

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Null)
                {
                    (strings ??= []).Add((string)reader.Value);
                }
                else
                {
                    numbers ??= capacityHint > 0 ? new List<double>(capacityHint) : [];
                    numbers.Add(Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
