using Newtonsoft.Json;

namespace ActDim.Three.NewtonsoftJson
{
    /// <summary>
    /// Convenient wrapper for Newtonsoft.Json serialization of <see cref="SceneDocument"/> and Three.js elements.
    /// </summary>
    public static class ThreeNewtonsoftSerializer
    {
        /// <summary>
        /// Creates default <see cref="JsonSerializerSettings"/> configured with <see cref="SceneDocumentConverter"/>,
        /// <see cref="BufferAttributeConverter"/>, <see cref="ElementConverter"/> and <see cref="CamelCaseCustomResolver"/>.
        /// </summary>
        public static JsonSerializerSettings CreateSettings(bool indented = false)
        {
            return new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new CamelCaseCustomResolver(),
                Converters = { new SceneDocumentConverter(), new BufferAttributeConverter(), new ElementConverter() },
            };
        }

        /// <summary>
        /// Serializes an object to JSON string using Newtonsoft.Json.
        /// </summary>
        public static string ToJson<T>(T value, bool indented = false)
        {
            return JsonConvert.SerializeObject(value, CreateSettings(indented));
        }

        /// <summary>
        /// Deserializes an object from JSON string using Newtonsoft.Json.
        /// </summary>
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, CreateSettings());
        }
    }
}
