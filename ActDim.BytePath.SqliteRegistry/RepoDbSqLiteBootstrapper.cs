using System.Threading;
using RepoDb;

namespace ActDim.BytePath
{
    /// <summary>
    /// Thread-safe bootstrapper for RepoDb SQLite provider initialization in SQLiteBlobRegistry.
    /// </summary>
    internal static class RepoDbSqLiteBootstrapper
    {
        private static int _sqLiteInitialized;

        /// <summary>
        /// Idempotently initializes the RepoDb SQLite provider.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.CompareExchange(ref _sqLiteInitialized, 1, 0) == 0)
            {
                GlobalConfiguration.Setup().UseSqlite();
            }
        }
    }
}
