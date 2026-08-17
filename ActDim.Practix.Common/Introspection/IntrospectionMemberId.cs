using System;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Record identifying a metadata member across assembly, module version, and metadata token.
    /// </summary>
    /// <param name="AssemblyFullName">The assembly full name.</param>
    /// <param name="ModuleVersionId">The module version GUID.</param>
    /// <param name="MetadataToken">The member metadata token.</param>
    public record class IntrospectionMemberId(string AssemblyFullName, Guid ModuleVersionId, int MetadataToken);
}
