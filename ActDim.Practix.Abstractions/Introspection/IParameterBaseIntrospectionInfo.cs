namespace ActDim.Practix.Abstractions.Introspection
{
    public interface IParameterBaseIntrospectionInfo : IBaseIntrospectionInfo
    {
        ITypeBaseIntrospectionInfo ParameterType { get; }
        int Position { get; }
        // TODO:
        // IsIn
        // IsOptional
        // IsOut
    }
}