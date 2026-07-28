using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using THREE.Core;

namespace THREE.Serialization
{
    /// <summary>
    /// Read-only converter that resolves the concrete <see cref="IElement"/> implementation from the
    /// <c>type</c> discriminator (e.g. "BufferGeometry", "MeshStandardMaterial") when deserializing the
    /// heterogeneous pools of a document (<c>geometries</c>, <c>materials</c>, …). Writing is left to the
    /// default serializer.
    /// <para>
    /// NOTE (plan §8/§12): this buffers each pool element via <see cref="JObject"/> because the three.js
    /// <c>type</c> field trails the payload (e.g. <c>data</c> comes first). That transiently boxes large
    /// numeric arrays during load; the final storage is still a typed buffer. A fully streaming document
    /// converter is a later milestone.
    /// </para>
    /// </summary>
    public class ElementConverter : JsonConverter
    {
        private static readonly Dictionary<string, Type> TypeMap = BuildTypeMap();

        private static Dictionary<string, Type> BuildTypeMap()
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var t in typeof(IElement).Assembly.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface)
                {
                    continue;
                }
                if (!typeof(IElement).IsAssignableFrom(t))
                {
                    continue;
                }
                if (t.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }
                map[t.Name] = t;
            }
            return map;
        }

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(IElement);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jo = JObject.Load(reader);
            var type = (string)jo["type"];

            if (type == null || !TypeMap.TryGetValue(type, out var concrete))
            {
                throw new JsonSerializationException($"Unknown element type '{type}'.");
            }

            var instance = Activator.CreateInstance(concrete);
            using (var subReader = jo.CreateReader())
            {
                serializer.Populate(subReader, instance);
            }
            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException($"{nameof(ElementConverter)} is read-only.");
        }
    }
}
