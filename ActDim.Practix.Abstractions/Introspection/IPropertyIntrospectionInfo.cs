namespace ActDim.Practix.Abstractions.Introspection
{
    public interface IPropertyIntrospectionInfo : IIntrospectionInfo
    {
        ITypeBaseIntrospectionInfo PropertyType { get; }

        bool IsStatic { get; }
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsProtected { get; }
        bool IsInternal { get; }
        bool IsProtectedInternal { get; }
        bool IsPrivateProtected { get; }
    }
}