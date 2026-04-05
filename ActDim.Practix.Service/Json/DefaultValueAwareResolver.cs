using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ActDim.Practix.Service.Json
{
    /// <summary>
    /// Honors [DefaultValue], [JsonDefaultValue], and [JsonIgnoreDefault] attributes on properties:
    /// <list type="bullet">
    ///   <item>[DefaultValue] — used as a default for [JsonIgnoreDefault] comparisons (no populate during deserialization).</item>
    ///   <item>[JsonDefaultValue] — used as the default for [JsonIgnoreDefault], and can populate during deserialization when Populate = true.</item>
    ///   <item>[JsonIgnoreDefault] — omits the property from JSON output when its value equals the default
    ///         (taken from [JsonDefaultValue] if present, otherwise [DefaultValue], otherwise the CLR default for the type).</item>
    /// </list>
    /// </summary>
    public class DefaultValueAwareResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);
            Apply(typeInfo);
            return typeInfo;
        }

        public static void Apply(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            var defaults = new List<(JsonPropertyInfo prop, object? defaultValue)>();

            foreach (var property in typeInfo.Properties)
            {
                var attrs = property.AttributeProvider?.GetCustomAttributes(true) ?? Array.Empty<object>();

                var defaultValueAttr = attrs.OfType<DefaultValueAttribute>().FirstOrDefault();
                var jsonDefaultValueAttr = attrs.OfType<JsonDefaultValueAttribute>().FirstOrDefault();
                var ignoreDefaultAttr = attrs.OfType<JsonIgnoreDefaultAttribute>().FirstOrDefault();

                // [JsonIgnoreDefault] — omit during serialization when value equals the default
                if (ignoreDefaultAttr != null)
                {
                    var ignoreValue =
                        jsonDefaultValueAttr != null ? jsonDefaultValueAttr.Value :
                        defaultValueAttr != null ? defaultValueAttr.Value :
                        GetTypeDefault(property.PropertyType);

                    var prevShouldSerialize = property.ShouldSerialize;
                    property.ShouldSerialize = (obj, value) =>
                    {
                        if (prevShouldSerialize != null && !prevShouldSerialize(obj, value))
                            return false;

                        return !Equals(value, ignoreValue);
                    };
                }

                // [JsonDefaultValue] — pre-set before deserialization when Populate = true
                if (jsonDefaultValueAttr != null && jsonDefaultValueAttr.Populate && property.Set != null)
                    defaults.Add((property, jsonDefaultValueAttr.Value));
            }

            if (defaults.Count == 0)
                return;

            var arr = defaults.ToArray();

            var previous = typeInfo.OnDeserializing;
            typeInfo.OnDeserializing = obj =>
            {
                previous?.Invoke(obj);
                foreach (var (prop, defaultValue) in arr)
                    prop.Set!(obj, defaultValue);
            };
        }

        private static object? GetTypeDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}


