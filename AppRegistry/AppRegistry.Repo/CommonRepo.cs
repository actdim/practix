using System.Data;
using System.Threading.Tasks;
using ActDim.AppRegistry.Domain.Core;
using RepoDb;

namespace ActDim.AppRegistry.Repo
{
    public class CommonRepo
    {
        public int ExpectedVersion = 1;
        private readonly IDbConnection? _connection;

        public CommonRepo(IDbConnection? connection = null)
        {
            _connection = connection;
        }

        private async Task<int> GetCurrentVersionAsync()
        {
            if (_connection != null)
            {
                var version = await _connection.ExecuteScalarAsync<int?>(
                    "SELECT schema_version FROM actdim.subsystems WHERE code = '@actdim/app-registry'");
                return version ?? ExpectedVersion;
            }

            return ExpectedVersion;
        }

        public async Task<VersionCheckResult> CheckVersionAsync()
        {
            return new VersionCheckResult()
            {
                CurrentVersion = await GetCurrentVersionAsync(),
                ExpectedVersion = ExpectedVersion
            };
        }
    }
}
