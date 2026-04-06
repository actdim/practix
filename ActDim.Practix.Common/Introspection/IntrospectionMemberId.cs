using System;

namespace ActDim.Practix.Introspection
{
    public record class IntrospectionMemberId(string AssemblyFullName, Guid ModuleVersionId, int MetadataToken);
}
