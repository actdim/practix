using System;

namespace ActDim.Practix.Common.Introspection
{
    public record class IntrospectionMemberId(string AssemblyFullName, Guid ModuleVersionId, int MetadataToken);
}
