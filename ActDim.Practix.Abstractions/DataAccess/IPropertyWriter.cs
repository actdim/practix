namespace ActDim.Practix.Abstractions.DataAccess
{
	public interface IPropertyWriter
	{
		void Write(object instance, string name, object value);
	}
}