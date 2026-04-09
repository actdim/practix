using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    internal interface IBlobRegistry
    {
        Task<BlobRecord> GetOrCreateAsync(string key, IBlobStoreOptions options, LockType lockType, CancellationToken ct);

        /// <summary>
        /// Acquires a shared read lease on the record. Throws <see cref="KeyNotFoundException"/> if not found.
        /// </summary>
        Task<BlobRecord> GetForReadingAsync(string key, CancellationToken ct);

        /// <summary>
        /// Acquires an exclusive write lease on the record. Throws <see cref="KeyNotFoundException"/> if not found.
        /// </summary>
        Task<BlobRecord> GetForWritingAsync(string key, CancellationToken ct);

        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        Task DeleteAsync(string key, CancellationToken ct);

        Task<int> DeleteExpiredAsync(CancellationToken ct);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);

        Task CleanupLocksAsync(CancellationToken ct);
    }
}
