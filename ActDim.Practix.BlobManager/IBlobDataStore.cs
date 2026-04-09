using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public interface IBlobDataStore
    {
        Task<string> ResolveLocationAsync(BlobRecord blobRecord, CancellationToken ct);
        Stream Append(BlobRecord blobRecord, Stream stream, long offset);
        Task<Stream> AppendAsync(BlobRecord blobRecord, Stream stream, long offset, CancellationToken ct);
        Stream Read(BlobRecord blobRecord);
        Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct);
        Stream Write(BlobRecord blobRecord);
        Task<Stream> WriteAsync(BlobRecord blobRecord, CancellationToken ct);
    }
}
