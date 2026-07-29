using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace THREE.Serialization
{
    /// <summary>
    /// A System.Text.Json type-info resolver that honors the WCF DataContract attributes the domain
    /// types are annotated with — <see cref="DataContractAttribute"/> (opt-in),
    /// <see cref="DataMemberAttribute"/> (<c>Name</c>) and <see cref="IgnoreDataMemberAttribute"/> — so
    /// STJ produces the same property names and member set as Newtonsoft. STJ does not understand these
    /// attributes on its own.
    /// <para>
    /// Usage: <c>new JsonSerializerOptions { TypeInfoResolver = DataContractResolver.Instance }</c>.
    /// Note: only public members are considered (STJ's default), so <c>internal [DataMember]</c> members
    /// are not emitted — a minor difference from Newtonsoft.
    /// </para>
    /// </summary>
    public sealed class DataContractResolver : DefaultJsonTypeInfoResolver
    {
        public static DataContractResolver Instance { get; } = new DataContractResolver();

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = base.GetTypeInfo(type, options);

            if (info.Kind != JsonTypeInfoKind.Object)
            {
                return info;
            }

            var optIn = HasDataContract(type);

            for (var i = info.Properties.Count - 1; i >= 0; i--)
            {
                var property = info.Properties[i];

                if (!(property.AttributeProvider is MemberInfo member))
                {
                    continue;
                }

                if (member.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                {
                    info.Properties.RemoveAt(i);
                    continue;
                }

                var dataMember = member.GetCustomAttribute<DataMemberAttribute>();

                // Opt-in ([DataContract] on the type or a base): only [DataMember] members are serialized.
                if (optIn && dataMember == null)
                {
                    info.Properties.RemoveAt(i);
                    continue;
                }

                if (dataMember != null && !string.IsNullOrEmpty(dataMember.Name))
                {
                    property.Name = dataMember.Name;
                }

                // Honor Newtonsoft-style ShouldSerialize<Name>() so STJ omits the same members
                // (e.g. empty `groups`/`morphAttributes`).
                var shouldSerialize = member.DeclaringType?.GetMethod("ShouldSerialize" + member.Name, Type.EmptyTypes);
                if (shouldSerialize != null && shouldSerialize.ReturnType == typeof(bool))
                {
                    property.ShouldSerialize = (obj, _) => obj != null && (bool)shouldSerialize.Invoke(obj, null);
                }
            }

            return info;
        }

        private static bool HasDataContract(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.GetCustomAttribute<DataContractAttribute>(inherit: false) != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
