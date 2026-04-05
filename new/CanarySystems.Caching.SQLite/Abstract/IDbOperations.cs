using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace CanarySystems.Caching.SQLite
{
    internal interface IDbOperations
    {
        CacheEntry GetCacheEntry(string key);

        Task<CacheEntry> GetCacheEntryAsync(string key, CancellationToken token = default(CancellationToken));

        void RefreshCacheEntry(string key);

        Task RefreshCacheEntryAsync(string key, CancellationToken token = default(CancellationToken));

        void DeleteCacheEntry(string key);

        Task DeleteCacheEntryAsync(string key, CancellationToken token = default(CancellationToken));

        void SetCacheEntry(string key, byte[] value, DistributedCacheEntryOptions options);

        Task SetCacheEntryAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));

        void DeleteExpiredCacheEntries();
    }
}