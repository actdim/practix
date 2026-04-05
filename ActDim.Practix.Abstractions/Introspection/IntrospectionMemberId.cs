namespace ActDim.Practix.Abstractions.Introspection
{
    public record class IntrospectionMemberId(string AssemblyFullName, Guid ModuleVersionId, int MetadataToken);    
}