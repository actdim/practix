namespace ActDim.Practix.Abstractions.DataAccess
{
	public interface IPropertyActivator
	{
		object CreateInstance(object context, string propertyName);
	}
}
