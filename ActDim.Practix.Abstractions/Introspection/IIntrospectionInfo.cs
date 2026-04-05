using System.Reflection;

namespace ActDim.Practix.Abstractions.Introspection
{
    /// <summary>
    /// IMemberIntrospectionInfo
    /// </summary>
    public interface IIntrospectionInfo : IBaseIntrospectionInfo
    {
        IntrospectionMemberId MemberId { get; }

        MemberTypes MemberType { get; }

        ITypeBaseIntrospectionInfo DeclaringType { get; }

        ITypeBaseIntrospectionInfo ReflectedType { get; }
    }
}