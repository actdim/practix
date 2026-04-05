namespace ActDim.Practix.Abstractions.Introspection
{
    public interface ITypeIntrospectionInfo : ITypeBaseIntrospectionInfo
    {
        IPropertyIntrospectionInfo[] Properties { get; }
        IFieldIntrospectionInfo[] Fields { get; }
        IMethodIntrospectionInfo[] Methods { get; }
        ITypeBaseIntrospectionInfo[] Interfaces { get; }
    }
}