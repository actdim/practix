using ActDim.Practix.Abstractions.Introspection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    /// <summary>
    /// MemberIntrospectionInfo
    /// </summary>
    [Serializable]
    public class IntrospectionInfo : BaseIntrospectionInfo, IIntrospectionInfo
    {
        internal static readonly ConditionalWeakTable<MemberInfo, IntrospectionInfo> Cache = [];

        public IntrospectionMemberId MemberId { get; }

        public MemberTypes MemberType { get; }

        public ITypeBaseIntrospectionInfo DeclaringType { get; }

        public ITypeBaseIntrospectionInfo ReflectedType { get; }

        // [JsonConstructor]
        public IntrospectionInfo(MemberInfo m) : base()
        {            
            Name = m.Name;
            DisplayName = m.Name; // TODO: Read from member's DisplayNameAttribute
            var type = m is Type t ? t : m.DeclaringType;
            MemberId = new IntrospectionMemberId(type.Assembly.FullName, type.Module.ModuleVersionId, m.MetadataToken);
            MemberType = m.MemberType;
            DeclaringType = (ITypeBaseIntrospectionInfo)m.DeclaringType?.GetIntrospectionInfo(false);
            ReflectedType = (ITypeBaseIntrospectionInfo)m.ReflectedType?.GetIntrospectionInfo(false);
        }
    }
}