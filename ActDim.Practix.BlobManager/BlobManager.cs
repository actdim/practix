using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    internal class BlobManager : IBlobManager
    {
        private readonly IBlobDataStore _dataStore;
        private readonly IBlobRegistry _registry;

        public BlobManager(IBlobDataStore dataStore, IBlobRegistry registry)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public IBlobDataStore DataStore => _dataStore;

        public Task DeleteAsync(string key, CancellationToken ct)
            => _registry.DeleteAsync(key, ct);

        public Task<int> DeleteExpiredAsync(CancellationToken ct)
            => _registry.DeleteExpiredAsync(ct);

        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
            => _registry.DeleteOlderThanAsync(cutoff, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForReadingAsync(string key, CancellationToken ct)
            => _registry.TryGetForReadingAsync(key, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct)
            => _registry.TryGetForReadingAsync(key, timeout, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForWritingAsync(string key, CancellationToken ct)
            => _registry.TryGetForWritingAsync(key, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct)
            => _registry.TryGetForWritingAsync(key, timeout, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetOrSetAsync(string key, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, null, LockType.Write, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetOrSetAsync(string key, TimeSpan timeout, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, null, LockType.Write, timeout, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetOrSetAsync(string key, IBlobStoreOptions options, LockType lockType, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, options, lockType, ct);

        public Task<(BlobErrorCode ErrorCode, BlobRecord Record)> TryGetOrSetAsync(string key, IBlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, options, lockType, timeout, ct);

        public Task<IList<string>> QueryAsync(string pattern, CancellationToken ct)
            => _registry.QueryAsync(pattern, ct);

        public async Task CleanupAsync(CancellationToken ct)
        {
            await _registry.CleanupLocksAsync(ct);
            await _registry.DeleteExpiredAsync(ct);
        }
    }
}
