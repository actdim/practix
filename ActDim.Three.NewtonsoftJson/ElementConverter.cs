using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ActDim.Three.Core;
using ActDim.Three.Serialization;

namespace ActDim.Three.NewtonsoftJson
{
    /// <summary>
    /// Read-only Newtonsoft converter that resolves the concrete <see cref="IElement"/> implementation from the
    /// <c>type</c> discriminator (e.g. "BufferGeometry", "MeshStandardMaterial") when deserializing heterogeneous pools.
    /// </summary>
    public class ElementConverter : JsonConverter
    {
        private static readonly JsonSerializer SubSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new CamelCaseCustomResolver(),
            Converters = { new BufferAttributeConverter() },
        });

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => typeof(IElement).IsAssignableFrom(objectType);

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            var typeToken = obj["type"] ?? obj["Type"];

            Type targetType = null;
            if (typeToken != null)
            {
                targetType = DocumentGraph.ElementType(typeToken.Value<string>());
            }

            if (targetType == null && !objectType.IsAbstract && !objectType.IsInterface)
            {
                targetType = objectType;
            }

            if (targetType == null)
            {
                throw new JsonSerializationException("Missing 'type' property when deserializing IElement.");
            }

            return obj.ToObject(targetType, SubSerializer);
        }
    }
}
