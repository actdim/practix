using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ActDim.Practix.Service.Json
{
    /// <summary>
    /// Extends DefaultJsonTypeInfoResolver to honor [JsonIgnoreEmpty] attributes:
    /// collection properties marked with [JsonIgnoreEmpty] are omitted from JSON output
    /// when the collection is null or empty.
    /// </summary>
    public class EmptyCollectionIgnoreResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);
            Apply(typeInfo);
            return typeInfo;
        }

        public static void Apply(JsonTypeInfo typeInfo)
        {
            foreach (var property in typeInfo.Properties)
            {
                var hasAttr = property.AttributeProvider?
                    .GetCustomAttributes(typeof(JsonIgnoreEmptyAttribute), inherit: true)
                    .Length > 0;

                if (!hasAttr)
                    continue;

                var existing = property.ShouldSerialize;

                property.ShouldSerialize = existing is null
                    ? (obj, val) => !IsEmpty(val)
                    : (obj, val) => existing(obj, val) && !IsEmpty(val);
            }
        }

        private static bool IsEmpty(object val)
        {
            if (val is null) return true;
            if (val is ICollection c) return c.Count == 0;
            if (val is IEnumerable e) return !e.Cast<object>().Any();
            return false;
        }
    }
}
