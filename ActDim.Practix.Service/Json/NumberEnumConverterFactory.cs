using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActDim.Practix.Service.Json
{
    /// <summary>
    /// Global factory that serializes all enum values as integers,
    /// matching Newtonsoft.Json's default enum behavior.
    /// Property-level converters (e.g. CamelCaseJsonEnumConverter) take precedence over this factory.
    /// Enums with their own [JsonConverter] attribute are skipped — type-level converters handle them.
    /// Note: in STJ, global Converters have higher priority than type-level [JsonConverter] attributes,
    /// so we must explicitly yield to types that declare their own converter.
    /// </summary>
    public class NumberEnumConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsEnum) return false;
            // Yield to types with their own [JsonConverter] — global converters would otherwise
            // shadow type-level attributes (STJ priority: property attr > global > type attr)
            return typeToConvert.GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false).Length == 0;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var factoryType = typeof(JsonNumberEnumConverter<>).MakeGenericType(typeToConvert);
            var factory = (JsonConverterFactory)Activator.CreateInstance(factoryType);
            return factory.CreateConverter(typeToConvert, options);
        }
    }
}
