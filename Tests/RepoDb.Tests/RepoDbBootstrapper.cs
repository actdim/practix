using System.Threading;
using RepoDb;

namespace ActDim.Practix.RepoDb.Tests
{
    /// <summary>
    /// Thread-safe bootstrapper for RepoDb SQLite provider initialization in tests.
    /// </summary>
    public static class RepoDbBootstrapper
    {
        private static int _sqLiteInitialized;

        /// <summary>
        /// Idempotently initializes the RepoDb SQLite provider.
        /// </summary>
        public static void InitializeSqLite()
        {
            if (Interlocked.CompareExchange(ref _sqLiteInitialized, 1, 0) == 0)
            {
                GlobalConfiguration.Setup().UseSqlite();
            }
        }
    }
}
