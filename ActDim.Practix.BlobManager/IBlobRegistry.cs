using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    internal interface IBlobRegistry
    {
        Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetOrSetAsync(string key, IBlobStoreOptions options, LockType lockType, CancellationToken ct);
        Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetOrSetAsync(string key, IBlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct);

        Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForReadingAsync(string key, CancellationToken ct);
        Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForWritingAsync(string key, CancellationToken ct);
        Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct);

        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        Task DeleteAsync(string key, CancellationToken ct);

        Task<int> DeleteExpiredAsync(CancellationToken ct);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);

        Task CleanupLocksAsync(CancellationToken ct);
    }
}
