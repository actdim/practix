using System;

namespace ActDim.Practix.Introspection
{
    /// <summary>
    /// Record identifying a metadata member across assembly, module version, metadata token, and member specification.
    /// </summary>
    /// <param name="AssemblyFullName">The assembly full name.</param>
    /// <param name="ModuleVersionId">The module version GUID.</param>
    /// <param name="MetadataToken">The member metadata token.</param>
    /// <param name="MemberSignature">The optional member specification or signature, differentiating constructed generic types, methods, and arrays.</param>
    public record class IntrospectionMemberId(string AssemblyFullName, Guid ModuleVersionId, int MetadataToken, string MemberSignature = null);
}
