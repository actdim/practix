using System;

namespace ActDim.Practix.Common.Introspection
{
    // [AttributeUsage(AttributeTargets.Class | ...)]
    // MemberIntrospectionInfoProviderAttribute
    public class IntrospectionInfoProviderAttribute : Attribute
    {
        public Type ProviderType { get; init; }
    }
}
