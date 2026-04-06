using System;

namespace SalientBits.Common.Reflection.Metadata
{
    // [AttributeUsage(AttributeTargets.Class | ...)]
    // MemberIntrospectionInfoProviderAttribute
    public class IntrospectionInfoProviderAttribute : Attribute
    {
        public Type ProviderType { get; init; }
    }
}