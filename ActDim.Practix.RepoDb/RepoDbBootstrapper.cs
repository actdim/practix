using System.Threading;
using RepoDb;

namespace ActDim.Practix.RepoDb
{
    /// <summary>
    /// Thread-safe bootstrapper for global RepoDb mapping and provider initializations.
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
