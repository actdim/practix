using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public interface IBlobManager
    {
        IBlobDataStore DataStore { get; }

        Task<BlobRecord> GetOrCreateAsync(string key, CancellationToken ct = default);

        Task<BlobRecord> GetOrCreateAsync(string key, IBlobStoreOptions options, LockType lockType = LockType.Write, CancellationToken ct = default);

        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        Task<BlobRecord> GetForReadingAsync(string key, CancellationToken ct);

        Task<BlobRecord> GetForWritingAsync(string key, CancellationToken ct);

        Task DeleteAsync(string key, CancellationToken ct = default);

        Task<int> DeleteExpiredAsync(CancellationToken ct = default);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);

        Task CleanupAsync(CancellationToken ct = default);
    }
}
