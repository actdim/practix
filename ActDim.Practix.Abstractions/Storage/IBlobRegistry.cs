using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BytePath
{
    /// <summary>
    /// Metadata and lock registry contract for ActDim BytePath.
    /// </summary>
    public interface IBlobRegistry
    {
        Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct);
        Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct);

        Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct);
        Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct);
        Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        /// <summary>
        /// Deletes the record and its locks. The caller must already hold the write lock on it,
        /// which <see cref="BlobRecord.LockType"/> attests - so no lock is acquired here.
        /// </summary>
        Task DeleteLockedAsync(BlobRecord record, CancellationToken ct);

        /// <summary>
        /// Drops every lock on the key so a subsequent acquisition cannot be blocked by a holder
        /// that is still alive. Only for the forced-deletion path.
        /// </summary>
        Task ForceUnlockAsync(string key, CancellationToken ct);

        Task<IList<string>> GetExpiredKeysAsync(CancellationToken ct);

        Task<IList<string>> GetKeysOlderThanAsync(DateTimeOffset cutoff, bool includeLocked, CancellationToken ct);

        Task CleanupLocksAsync(CancellationToken ct);
    }
}
