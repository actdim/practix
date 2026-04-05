namespace ActDim.Practix.Abstractions.Introspection
{
    public interface IMethodIntrospectionInfo : IIntrospectionInfo
    {
        bool IsConstructor { get; }
        bool IsAbstract { get; }
        bool IsVirtual { get; }
        bool IsStatic { get; }
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsProtected { get; }
        bool IsInternal { get; }
        bool IsProtectedInternal { get; }
        bool IsPrivateProtected { get; }
        bool IsGeneric { get; }
        bool IsGenericDefinition { get; }
        /// <summary>
        /// Generic Type Parameters
        /// </summary>
        ITypeBaseIntrospectionInfo[] GenericParameters { get; }
        /// <summary>
        /// Generic Type Arguments
        /// </summary>
        ITypeBaseIntrospectionInfo[] GenericArguments { get; }
        IParameterBaseIntrospectionInfo[] Parameters { get; }
        ITypeBaseIntrospectionInfo ReturnType { get; }
    }
}