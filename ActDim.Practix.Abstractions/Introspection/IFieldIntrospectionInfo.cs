namespace ActDim.Practix.Abstractions.Introspection
{
	public interface IFieldIntrospectionInfo: IIntrospectionInfo
	{
        ITypeBaseIntrospectionInfo FieldType { get; }
	}
}