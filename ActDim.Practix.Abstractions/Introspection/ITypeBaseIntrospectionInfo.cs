namespace ActDim.Practix.Abstractions.Introspection
{
    public interface ITypeBaseIntrospectionInfo : IIntrospectionInfo
    {
        string FullName { get; }
        string Namespace { get; }
        string AssemblyQualifiedName { get; }
        bool IsClass { get; }
        bool IsInterface { get; }
        bool IsAbstract { get; }
        bool IsSealed { get; }
        bool IsStatic { get; }
        bool IsEnum { get; }
        bool IsValueType { get; }
        bool IsPrimitive { get; }
        bool IsGeneric { get; }
        bool IsGenericDefinition { get; }
        bool IsNested { get; }
        bool IsNotPublic { get; }
        bool IsPublic { get; }
        bool IsArray { get; }
        bool IsPointer { get; }
        bool IsByRef { get; }
        ITypeBaseIntrospectionInfo BaseType { get; }
        ITypeBaseIntrospectionInfo ElementType { get; }
        /// <summary>
        /// Generic Type Parameters
        /// </summary>
        ITypeBaseIntrospectionInfo[] GenericParameters { get; }
        /// <summary>
        /// Generic Type Arguments
        /// </summary>
        ITypeBaseIntrospectionInfo[] GenericArguments { get; }
    }
}