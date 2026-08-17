using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// Extends <see cref="DefaultJsonTypeInfoResolver"/> to apply custom <see cref="JsonNamingAttribute"/> policies to serialized type properties.
    /// </summary>
    public class NamingPolicyResolver : DefaultJsonTypeInfoResolver
    {
        /// <inheritdoc />
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = base.GetTypeInfo(type, options);
            Apply(info);
            return info;
        }

        /// <summary>
        /// Applies the naming policy defined by <see cref="JsonNamingAttribute"/> to the specified <paramref name="info"/>.
        /// </summary>
        /// <param name="info">The target JSON type information metadata.</param>
        public static void Apply(JsonTypeInfo info)
        {
            var attr = info.Type.GetCustomAttribute<JsonNamingAttribute>();
            if (attr == null)
            {
                return;
            }

            foreach (var prop in info.Properties)
            {
                var hasExplicitName = prop.AttributeProvider?
                    .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                    .Length > 0;

                if (hasExplicitName)
                {
                    continue;
                }

                if (prop.AttributeProvider is MemberInfo member)
                {
                    var clrName = member.Name;
                    prop.Name = attr.Policy != null ? attr.Policy.ConvertName(clrName) : clrName;
                }
            }
        }
    }
}
