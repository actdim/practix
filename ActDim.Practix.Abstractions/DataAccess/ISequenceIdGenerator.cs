using System.Data.Common;

namespace ActDim.Practix.Abstractions.DataAccess
{
    public interface ISequenceIdGenerator
    {
        DbProviderType ProviderType { get; }

        long GetNewId(string sequenceName);
        
        /// <summary>
        /// GetNext
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sequenceName"></param>
        /// <returns></returns>
        long GetNewId(DbConnection connection, string sequenceName);
    }
}
