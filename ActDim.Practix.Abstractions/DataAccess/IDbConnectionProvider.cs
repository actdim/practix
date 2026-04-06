using System.Data.Common;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.DataAccess
{
    // IDbConnectionFactory
    public interface IDbConnectionProvider
    {
        // GetConnection
        DbConnection CreateConnection(string connString);

        // DbProviderType GetProviderType(DbConnection connection);

        DbProviderType GetProviderType(string connString);

        // ClearConnectionPoolAsync

        Task ClearPoolAsync(string connString);
    }
}
