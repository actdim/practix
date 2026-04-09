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

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            return _registry.DeleteAsync(key, ct);
        }

        public Task<int> DeleteExpiredAsync(CancellationToken ct = default)
        {
            return _registry.DeleteExpiredAsync(ct);
        }

        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
        {
            return _registry.DeleteOlderThanAsync(cutoff, ct);
        }

        public Task<BlobRecord> GetForReadingAsync(string key, CancellationToken ct)
        {
            return _registry.GetForReadingAsync(key, ct);
        }

        public Task<BlobRecord> GetForWritingAsync(string key, CancellationToken ct)
        {
            return _registry.GetForWritingAsync(key, ct);
        }

        public Task<BlobRecord> GetOrCreateAsync(string key, CancellationToken ct = default)
        {
            return _registry.GetOrCreateAsync(key, null, LockType.Write, ct);
        }

        public Task<BlobRecord> GetOrCreateAsync(string key, IBlobStoreOptions options, LockType lockType = LockType.Write, CancellationToken ct = default)
        {
            return _registry.GetOrCreateAsync(key, options, lockType, ct);
        }

        public Task<IList<string>> QueryAsync(string pattern, CancellationToken ct)
        {
            return _registry.QueryAsync(pattern, ct);
        }

        public async Task CleanupAsync(CancellationToken ct = default)
        {
            await _registry.CleanupLocksAsync(ct);
            await _registry.DeleteExpiredAsync(ct);
        }
    }
}
