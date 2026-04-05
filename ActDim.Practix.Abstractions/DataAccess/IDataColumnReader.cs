using System.Data.Common;

namespace ActDim.Practix.Abstractions.DataAccess
{
	public interface IDataColumnReader
	{
		object Read(DbDataReader reader, int ordinal);
	}
}
