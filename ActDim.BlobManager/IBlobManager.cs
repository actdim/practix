using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BlobManager
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

        /// <summary>
        /// Acquires the write lock on an existing blob and applies <paramref name="options"/> to the
        /// record it hands out — the same thing as calling <see cref="BlobRecord.Apply"/> on the
        /// result, spelled as one call. The values reach storage when the record is disposed, so a
        /// caller that never disposes the handle persists nothing, exactly as with any other
        /// mutation made through it.
        /// </summary>
        Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, CancellationToken ct);
        Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, TimeSpan timeout, CancellationToken ct);

        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        Task DeleteAsync(string key, CancellationToken ct);
        Task DeleteAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<int> DeleteExpiredAsync(CancellationToken ct);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct, bool forceDeleteLocked = false);

        Task CleanupAsync(CancellationToken ct);
    }
}
