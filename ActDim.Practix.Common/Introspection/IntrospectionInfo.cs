using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Introspection
{
    /// <summary>
    /// MemberIntrospectionInfo
    /// </summary>
    public class IntrospectionInfo : BaseIntrospectionInfo
    {
        internal static readonly ConditionalWeakTable<MemberInfo, IntrospectionInfo> Cache = [];

        public IntrospectionMemberId MemberId { get; set; }

        public MemberTypes MemberType { get; set; }

        public TypeBaseIntrospectionInfo DeclaringType { get; set; }

        public TypeBaseIntrospectionInfo ReflectedType { get; set; }

        public IntrospectionInfo() { }

        public IntrospectionInfo(MemberInfo m) : base()
        {
            Name = m.Name;
            DisplayName = m.Name;
            var type = m is Type t ? t : m.DeclaringType;
            MemberId = new IntrospectionMemberId(type.Assembly.FullName, type.Module.ModuleVersionId, m.MetadataToken);
            MemberType = m.MemberType;
            DeclaringType = (TypeBaseIntrospectionInfo)m.DeclaringType?.GetIntrospectionInfo(false);
            ReflectedType = (TypeBaseIntrospectionInfo)m.ReflectedType?.GetIntrospectionInfo(false);
        }
    }
}
