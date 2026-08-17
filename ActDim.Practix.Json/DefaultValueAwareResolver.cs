using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Honors <see cref="DefaultValueAttribute"/>, <see cref="JsonDefaultValueAttribute"/>, and <see cref="JsonIgnoreDefaultAttribute"/> attributes on properties:
    /// <list type="bullet">
    ///   <item><see cref="DefaultValueAttribute"/> — used as a default for <see cref="JsonIgnoreDefaultAttribute"/> comparisons (no populate during deserialization).</item>
    ///   <item><see cref="JsonDefaultValueAttribute"/> — used as the default for <see cref="JsonIgnoreDefaultAttribute"/>, and can populate during deserialization when Populate = true.</item>
    ///   <item><see cref="JsonIgnoreDefaultAttribute"/> — omits the property from JSON output when its value equals the default
    ///         (taken from <see cref="JsonDefaultValueAttribute"/> if present, otherwise <see cref="DefaultValueAttribute"/>, otherwise the CLR default for the type).</item>
    /// </list>
    /// </summary>
    public class DefaultValueAwareResolver : DefaultJsonTypeInfoResolver
    {
        /// <inheritdoc />
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);
            Apply(typeInfo);
            return typeInfo;
        }

        /// <summary>
        /// Applies default-value resolution and serialization suppression rules to the specified <paramref name="typeInfo"/>.
        /// </summary>
        /// <param name="typeInfo">The JSON type information metadata.</param>
        public static void Apply(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }

            var defaults = new List<(JsonPropertyInfo prop, object defaultValue)>();

            foreach (var property in typeInfo.Properties)
            {
                var attrs = property.AttributeProvider?.GetCustomAttributes(true) ?? [];

                var defaultValueAttr = attrs.OfType<DefaultValueAttribute>().FirstOrDefault();
                var jsonDefaultValueAttr = attrs.OfType<JsonDefaultValueAttribute>().FirstOrDefault();
                var ignoreDefaultAttr = attrs.OfType<JsonIgnoreDefaultAttribute>().FirstOrDefault();

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
                        {
                            return false;
                        }

                        return !Equals(value, ignoreValue);
                    };
                }

                if (jsonDefaultValueAttr != null && jsonDefaultValueAttr.Populate && property.Set != null)
                {
                    defaults.Add((property, jsonDefaultValueAttr.Value));
                }
            }

            if (defaults.Count == 0)
            {
                return;
            }

            var arr = defaults.ToArray();

            var previous = typeInfo.OnDeserializing;
            typeInfo.OnDeserializing = obj =>
            {
                previous?.Invoke(obj);
                foreach (var (prop, defaultValue) in arr)
                {
                    prop.Set!(obj, defaultValue);
                }
            };
        }

        private static object GetTypeDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
