using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public interface IBlobDataStore
    {
        Task<string> ResolveLocationAsync(BlobRecord blobRecord, CancellationToken ct);
        Task<bool> ExistsAsync(BlobRecord blobRecord, CancellationToken ct);
        Task<Stream> CreateAsync(BlobRecord blobRecord, CancellationToken ct);
        Task<Stream> WriteAsync(BlobRecord blobRecord, CancellationToken ct);
        Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct);
        Task<Stream> AppendAsync(BlobRecord blobRecord, long offset, CancellationToken ct);
    }
}
