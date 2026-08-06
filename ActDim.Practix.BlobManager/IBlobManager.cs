using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public interface IBlobManager
    {
        IBlobDataStore DataStore { get; }

        Task<BlobResult> TryGetOrSetAsync(string key, CancellationToken ct);
        Task<BlobResult> TryGetOrSetAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct);
        Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct);

        Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct);
        Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct);
        Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        Task DeleteAsync(string key, CancellationToken ct);
        Task DeleteAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<int> DeleteExpiredAsync(CancellationToken ct);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct, bool forceDeleteLocked = false);

        Task CleanupAsync(CancellationToken ct);
    }
}
