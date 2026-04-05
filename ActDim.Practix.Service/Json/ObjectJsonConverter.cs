using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Json
{
    /// <summary>
    /// Deserializes object-typed properties to CLR primitives, mimicking Newtonsoft behavior:
    /// JSON number → long or double, string → string, bool → bool,
    /// object → ExpandoObject (supports dynamic access), array → List&lt;object&gt;.
    /// </summary>
    public class ObjectJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(object);

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadValue(ref reader, options);

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
                list.Add(ReadValue(ref reader, options));
            return list;
        }

        /*
        private static DynamicArray ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var list = new List<object?>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                list.Add(ReadValue(ref reader, options));
            return new DynamicArray(list);
        }
        */
    }
    /*
    public class DynamicArray : DynamicObject, IEnumerable<object?>
    {
        private readonly List<object> _items;

        public DynamicArray(List<object> items) => _items = items;

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = _items[(int)indexes[0]];
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = binder.Name switch
            {
                "Count" => _items.Count,
                "Length" => _items.Count,
                _ => null
            };
            return result != null;
        }

        public int Count => _items.Count;
        public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
    */
}
