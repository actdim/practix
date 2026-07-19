using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Repo
{
    public class CommonRepo
    {
        public int ExpectedVersion = 1;

        private static readonly string GetDbVersionCommandText =
            @"SELECT db_version FROM public.db_info";

        private async Task<int> GetCurrentVersionAsync()
        {
            int version = 0;

            // TODO: implement

            return version;

            // return ExpectedVersion;
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