namespace ActDim.Practix.Abstractions.DataAccess
{
	public interface IColumnSerializer
	{
		object Serialize(object source);
		object Deserialize(object source);
	}
}
