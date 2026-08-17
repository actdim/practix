using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Base introspection information DTO model for reflection members.
    /// </summary>
    public class IntrospectionInfo : BaseIntrospectionInfo
    {
        internal static readonly ConditionalWeakTable<MemberInfo, IntrospectionInfo> Cache = [];

        /// <summary>Gets or sets the unique member identifier.</summary>
        public IntrospectionMemberId MemberId { get; set; }

        /// <summary>Gets or sets the reflection member type.</summary>
        public MemberTypes MemberType { get; set; }

        /// <summary>Gets or sets declaring type introspection info.</summary>
        public TypeBaseIntrospectionInfo DeclaringType { get; set; }

        /// <summary>Gets or sets reflected type introspection info.</summary>
        public TypeBaseIntrospectionInfo ReflectedType { get; set; }

        /// <summary>Initializes a new instance of the <see cref="IntrospectionInfo"/> class.</summary>
        public IntrospectionInfo() { }

        /// <summary>Initializes a new instance of the <see cref="IntrospectionInfo"/> class from a member info.</summary>
        /// <param name="m">The target member info.</param>
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
