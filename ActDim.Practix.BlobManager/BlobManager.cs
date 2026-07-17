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

        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct, bool forceDeleteLocked = false)
            => _registry.DeleteOlderThanAsync(cutoff, ct, forceDeleteLocked);

        public async Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct)
        {
            var blobResult = await _registry.TryGetForReadingAsync(key, ct);
            return await VerifyExistsAsync(blobResult, ct);
        }

        public async Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            var blobResult = await _registry.TryGetForReadingAsync(key, timeout, ct);
            return await VerifyExistsAsync(blobResult, ct);
        }

        public async Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct)
        {
            var blobResult = await _registry.TryGetForWritingAsync(key, ct);
            return await VerifyExistsAsync(blobResult, ct);
        }

        public async Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            var blobResult = await _registry.TryGetForWritingAsync(key, timeout, ct);
            return await VerifyExistsAsync(blobResult, ct);
        }

        public Task<BlobResult> TryGetOrSetAsync(string key, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, null, LockType.Write, ct);

        public Task<BlobResult> TryGetOrSetAsync(string key, TimeSpan timeout, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, null, LockType.Write, timeout, ct);

        public Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, options, lockType, ct);

        public Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct)
            => _registry.TryGetOrSetAsync(key, options, lockType, timeout, ct);

        public Task<IList<string>> QueryAsync(string pattern, CancellationToken ct)
            => _registry.QueryAsync(pattern, ct);

        public async Task CleanupAsync(CancellationToken ct)
        {
            await _registry.CleanupLocksAsync(ct);
            await _registry.DeleteExpiredAsync(ct);
        }

        private async Task<BlobResult> VerifyExistsAsync(BlobResult blobResult, CancellationToken ct)
        {
            if (!blobResult.IsSuccess)
                return blobResult;

            try
            {
                var exists = await _dataStore.ExistsAsync(blobResult.Record, ct);
                if (exists)
                    return blobResult;
            }
            catch
            {
                await blobResult.DisposeAsync();
                throw;
            }

            await blobResult.DisposeAsync();
            await _registry.DeleteAsync(blobResult.Record.Key, ct);
            return new BlobResult(BlobErrorCode.KeyNotFound);
        }
    }
}
