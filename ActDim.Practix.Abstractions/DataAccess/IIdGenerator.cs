namespace ActDim.Practix.Abstractions.DataAccess
{
	public interface IIdGenerator
	{
		long GetNewId(string sequenceName);
		long GetNewId<T>();
	}
}
