using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BytePath
{
    internal class BlobManager : IBlobManager
    {
        private readonly List<IBlobDataStore> _dataStores;
        private readonly IBlobRegistry _registry;

        public BlobManager(IBlobDataStore dataStore, IBlobRegistry registry)
            : this(dataStore != null ? new[] { dataStore } : null, registry)
        {
        }

        public BlobManager(IEnumerable<IBlobDataStore> dataStores, IBlobRegistry registry)
        {
            if (dataStores == null)
            {
                throw new ArgumentNullException(nameof(dataStores));
            }

            _dataStores = new List<IBlobDataStore>(dataStores);
            if (_dataStores.Count == 0)
            {
                throw new ArgumentException("At least one data store must be registered.", nameof(dataStores));
            }

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <inheritdoc />
        public IBlobDataStore DataStore => _dataStores[0];

        /// <inheritdoc />
        public IReadOnlyList<IBlobDataStore> DataStores => _dataStores;

        /// <inheritdoc />
        public IBlobDataStore GetDataStore(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (TryGetDataStore(key, out var store))
            {
                return store;
            }

            throw new NotSupportedException($"No data store registered to handle key prefix for '{key}'.");
        }

        private bool TryGetDataStore(string key, out IBlobDataStore dataStore)
        {
            if (key == null)
            {
                dataStore = null;
                return false;
            }

            // 1. Longest non-empty prefix match first
            IBlobDataStore bestMatch = null;
            var bestPrefixLength = -1;

            for (var i = 0; i < _dataStores.Count; i++)
            {
                var store = _dataStores[i];
                var prefix = store.KeyPrefix;
                if (!string.IsNullOrEmpty(prefix) && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    if (prefix.Length > bestPrefixLength)
                    {
                        bestPrefixLength = prefix.Length;
                        bestMatch = store;
                    }
                }
            }

            if (bestMatch != null)
            {
                dataStore = bestMatch;
                return true;
            }

            // 2. Catch-all (empty or null KeyPrefix) fallback
            for (var i = 0; i < _dataStores.Count; i++)
            {
                var store = _dataStores[i];
                if (string.IsNullOrEmpty(store.KeyPrefix))
                {
                    dataStore = store;
                    return true;
                }
            }

            dataStore = null;
            return false;
        }

        /// <inheritdoc />
        public Task DeleteAsync(string key, CancellationToken ct)
            => DeleteCoreAsync(key, null, ct);

        /// <inheritdoc />
        public Task DeleteAsync(string key, TimeSpan timeout, CancellationToken ct)
            => DeleteCoreAsync(key, timeout, ct);

        /// <inheritdoc />
        public async Task<int> DeleteExpiredAsync(CancellationToken ct)
            => await DeleteManyAsync(await _registry.GetExpiredKeysAsync(ct), false, ct);

        /// <inheritdoc />
        public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct, bool forceDeleteLocked = false)
            => await DeleteManyAsync(
                await _registry.GetKeysOlderThanAsync(cutoff, forceDeleteLocked, ct),
                forceDeleteLocked,
                ct);

        /// <summary>
        /// Deletes content before metadata. A leftover registry row is recoverable — it is
        /// reported as new and pruned on the next access — whereas a leftover file is invisible
        /// to the library and lost for good.
        /// </summary>
        private async Task DeleteCoreAsync(string key, TimeSpan? timeout, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                throw new NotSupportedException($"No data store registered to handle key prefix for '{key}'.");
            }

            // Straight to the registry rather than through TryGetForWritingAsync: reconciliation
            // would prune a record whose content is already gone and report KeyNotFound, while
            // deleting such a record is exactly what is being asked for here.
            var blobResult = timeout.HasValue
                ? await _registry.TryGetForWritingAsync(key, timeout.Value, ct)
                : await _registry.TryGetForWritingAsync(key, ct);

            if (blobResult.ErrorCode == BlobErrorCode.KeyNotFound)
            {
                throw new KeyNotFoundException($"Blob '{key}' not found.");
            }

            if (blobResult.ErrorCode == BlobErrorCode.Timeout)
            {
                throw new TimeoutException($"Timeout while acquiring write lock for '{key}'.");
            }

            try
            {
                await dataStore.DeleteAsync(blobResult.Record, ct);
                await _registry.DeleteLockedAsync(blobResult.Record, ct);
            }
            finally
            {
                await blobResult.DisposeAsync();
            }
        }

        private async Task<int> DeleteManyAsync(IList<string> keys, bool forceDeleteLocked, CancellationToken ct)
        {
            var deleted = 0;

            foreach (var key in keys)
            {
                if (forceDeleteLocked)
                {
                    await _registry.ForceUnlockAsync(key, ct);
                }

                try
                {
                    // The candidates were selected as unlocked, so there is nothing to wait for:
                    // TimeSpan.Zero attempts the lock once and anything locked since then is
                    // skipped rather than waited on.
                    await DeleteCoreAsync(key, TimeSpan.Zero, ct);
                    deleted++;
                }
                catch (TimeoutException)
                {
                    // Locked again between selection and deletion — leave it for the next sweep.
                }
                catch (KeyNotFoundException)
                {
                    // Deleted by someone else in the meantime.
                }
                catch (NotSupportedException)
                {
                    // Store prefix no longer configured — skip.
                }
            }

            return deleted;
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetForReadingAsync(key, ct), dataStore, false, null, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetForReadingAsync(key, timeout, ct), dataStore, false, timeout, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetForWritingAsync(key, ct), dataStore, false, null, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetForWritingAsync(key, timeout, ct), dataStore, false, timeout, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, CancellationToken ct)
            => ApplyOptions(await TryGetForWritingAsync(key, ct), options);

        /// <inheritdoc />
        public async Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, TimeSpan timeout, CancellationToken ct)
            => ApplyOptions(await TryGetForWritingAsync(key, timeout, ct), options);

        /// <summary>
        /// Applies the caller's options to a record handed out under a write lock. Unlike
        /// <see cref="TryGetOrSetAsync(string, BlobStoreOptions, LockType, CancellationToken)"/>, which
        /// has to persist them before it may downgrade the lock, nothing is written here: the write
        /// lock is held until the handle is disposed, and disposal persists the record anyway.
        /// </summary>
        private static BlobResult ApplyOptions(BlobResult blobResult, BlobStoreOptions options)
        {
            if (options != null && blobResult.IsSuccess)
            {
                blobResult.Record.Apply(options);
            }

            return blobResult;
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetOrSetAsync(string key, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetOrSetAsync(key, null, LockType.Write, ct), dataStore, true, null, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetOrSetAsync(string key, TimeSpan timeout, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetOrSetAsync(key, null, LockType.Write, timeout, ct), dataStore, true, timeout, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetOrSetAsync(key, options, lockType, ct), dataStore, true, null, ct);
        }

        /// <inheritdoc />
        public async Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct)
        {
            if (!TryGetDataStore(key, out var dataStore))
            {
                return new BlobResult(BlobErrorCode.UnsupportedKeyPrefix);
            }

            return await ReconcileContentAsync(await _registry.TryGetOrSetAsync(key, options, lockType, timeout, ct), dataStore, true, timeout, ct);
        }

        private async Task<BlobResult> ReconcileContentAsync(BlobResult blobResult, IBlobDataStore dataStore, bool allowNew, TimeSpan? timeout, CancellationToken ct)
        {
            if (!blobResult.IsSuccess)
            {
                return blobResult;
            }

            if (blobResult.IsNew && !allowNew)
            {
                throw new InvalidOperationException($"Invalid record state: {blobResult.Record.Key}");
            }

            long? size;
            try
            {
                size = await dataStore.GetSizeAsync(blobResult.Record, ct);
            }
            catch
            {
                await blobResult.DisposeAsync();
                throw;
            }

            // The size comes from the data store rather than from the registry, because the
            // registry only ever stores what a previous handle happened to persist. The record
            // is handed out under a lock, so this value stays valid for the lifetime of the
            // handle unless the caller writes through it — see TrackSizeOnDispose.
            blobResult.Record.Size = size;

            if (size.HasValue)
            {
                return TrackSizeOnDispose(blobResult, dataStore);
            }

            if (allowNew)
            {
                // Either a record the registry has just created, or one that outlived its
                // content. Either way the caller has to produce the content, so report it as
                // new; the lock we already hold stays untouched.
                blobResult.IsNew = true;
                return TrackSizeOnDispose(blobResult, dataStore);
            }

            // The caller asked for existing content, so drop the orphaned record.
            var key = blobResult.Record.Key;
            await blobResult.DisposeAsync();

            try
            {
                await DeleteCoreAsync(key, timeout, ct);
            }
            catch (TimeoutException)
            {
                // Another participant holds the lock, so whether the record is really orphaned
                // could not be established — reporting KeyNotFound here would be a guess.
                // The record stays behind for CleanupAsync or a later retry.
                return new BlobResult(BlobErrorCode.Timeout);
            }
            catch (KeyNotFoundException)
            {
                // Already gone, which is the outcome being reported anyway.
            }

            return new BlobResult(BlobErrorCode.KeyNotFound);
        }

        /// <summary>
        /// Re-reads the content size when a write-locked record is released, so the value the
        /// registry persists matches the data store. Requires the caller to dispose the write
        /// stream before the record, which is the documented usage pattern.
        /// </summary>
        private static BlobResult TrackSizeOnDispose(BlobResult blobResult, IBlobDataStore dataStore)
        {
            var record = blobResult.Record;
            if (record.LockType != LockType.Write)
            {
                // Content cannot change under a read lock, so the size read on hand-out holds.
                return blobResult;
            }

            var releaseAsync = record.OnDisposeAsync;
            record.OnDisposeAsync = async () =>
            {
                record.Size = await dataStore.GetSizeAsync(record, CancellationToken.None);
                if (releaseAsync != null)
                {
                    await releaseAsync();
                }
            };

            return blobResult;
        }

        /// <inheritdoc />
        public Task<IList<string>> QueryAsync(string pattern, CancellationToken ct)
            => _registry.QueryAsync(pattern, ct);

        /// <inheritdoc />
        public async Task CleanupAsync(CancellationToken ct)
        {
            await _registry.CleanupLocksAsync(ct);
            await DeleteExpiredAsync(ct);
        }
    }
}
