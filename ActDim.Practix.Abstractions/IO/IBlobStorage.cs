using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.IO
{
    public interface IBlobStorage
    {
        Task<IBlob> GetAsync(string key, CancellationToken cancellationToken = default);        

        // IAsyncEnumerable<IBlob> FindAsync(string pattern, CancellationToken cancellationToken = default);

        Task<IList<IBlob>> FindAsync(string pattern, CancellationToken cancellationToken = default); // GLOB/REGEXP patterns

        Task SaveAsync(string key, Stream data, IStorageOptions options, CancellationToken cancellationToken = default);

        Task SaveAsync(string key, ReadOnlyMemory<byte> data, IStorageOptions options, CancellationToken cancellationToken = default);

        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
        
        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    }
}