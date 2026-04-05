using Npgsql;
using System.Data;
using System.Data.Common;

namespace Orthobits.Abstractions.DataAccess
{
    // PgDbConnectionFactory
    public class PgDbConnectionProvider : IDbConnectionProvider
    {
        public DbConnection CreateConnection(string connString)
        {
            // FormatException?
            // throw new NotSupportedException($"Connection string is not supported or invalid");
            
            // $"Server={host};Port=5432;Database={db};User Id={user};Password={password};Keepalive=30;Minimum Pool Size=0;Timeout={60};CommandTimeout={60*60};";

            // $"Data Source={Path.Combine(directoryPath, db)}.db;Version=3;Pooling=True;Max Pool Size=100;";

            // DB requires regular maintenance and occasional repairs to ensure optimal performance
            // throw new InvalidOperationException($"Database maintenance is in progress");            
            return new NpgsqlConnection(connString);
        }

        public DbProviderType GetProviderType(string connString)
        {
            return DbProviderType.PostgreSQL;
        }

        public async Task ClearPoolAsync(string connString)
        {
            using (var connection = new NpgsqlConnection(connString))
            {
                if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }
                connection.ReloadTypes();
                NpgsqlConnection.ClearPool(connection);
            }
        }

    }
}
