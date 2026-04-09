using System.Data.Common;

namespace ActDim.Practix.Abstractions.DataAccess
{
	public interface IDbOperation
	{
		DbCommand CreateCommand(DbConnection connection);
	}
}
