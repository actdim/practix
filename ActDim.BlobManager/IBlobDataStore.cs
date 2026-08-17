using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BlobManager
{
    public interface IBlobDataStore
    {
        Task<string> ResolveLocationAsync(BlobRecord blobRecord, CancellationToken ct);

        /// <summary>
        /// Returns the current size of the stored content in bytes, or null when there is no
        /// content for the record. Doubles as the existence check: one round trip to the
        /// underlying storage answers both questions.
        /// </summary>
        Task<long?> GetSizeAsync(BlobRecord blobRecord, CancellationToken ct);

        /// <summary>
        /// Whether content exists for the record. Derived from <see cref="GetSizeAsync"/> so that a
        /// store has a single primitive to implement and the two can never disagree. Override only
        /// for a backend that has a genuinely cheaper existence probe.
        /// </summary>
        async Task<bool> ExistsAsync(BlobRecord blobRecord, CancellationToken ct)
            => (await GetSizeAsync(blobRecord, ct)).HasValue;

        /// <summary>
        /// Stores the content as a whole, creating it when absent and discarding whatever was
        /// there otherwise. Returns the resulting size.
        /// </summary>
        /// <remarks>
        /// The write is complete when this returns; nothing is left to commit later.
        /// <paramref name="content"/> is read at whatever rate it produces, so an incremental producer
        /// needs no buffering. A producer that can only write into a stream should use the
        /// <paramref name="produce"/> overload instead.
        /// </remarks>
        Task<long> PutAsync(BlobRecord blobRecord, Stream content, CancellationToken ct);

        /// <summary>
        /// Writes the content past its current end, creating it when absent. Returns the resulting
        /// total size. The caller does not need to know the current size.
        /// </summary>
        Task<long> AppendAsync(BlobRecord blobRecord, Stream content, CancellationToken ct);

        /// <summary>
        /// Stores the content as a whole, letting <paramref name="produce"/> write it into a stream
        /// supplied to it — for producers that can only write and have no readable form to hand over.
        /// Returns the resulting size.
        /// </summary>
        /// <remarks>
        /// Returning from <paramref name="produce"/> is what completes the content, so there is nothing
        /// left to close afterwards. The supplied stream is write-only and must not be assumed seekable.
        /// Flush any writer wrapped around it before returning, or its buffer is lost.
        /// </remarks>
        Task<long> PutAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, CancellationToken ct)
            => ProducerStreamBridge.PutAsync(this, blobRecord, produce, ct);

        /// <summary>
        /// Writes content past the current end, letting <paramref name="produce"/> write it into a
        /// stream supplied to it. Returns the resulting total size.
        /// </summary>
        Task<long> AppendAsync(BlobRecord blobRecord, Func<Stream, CancellationToken, Task> produce, CancellationToken ct)
            => ProducerStreamBridge.AppendAsync(this, blobRecord, produce, ct);

        /// <summary>
        /// Opens the content for reading. The returned stream is seekable, so a range can be read by
        /// seeking to it.
        /// </summary>
        Task<Stream> ReadAsync(BlobRecord blobRecord, CancellationToken ct);

        /// <summary>
        /// Removes the stored content. Returns false when there was nothing to remove.
        /// </summary>
        Task<bool> DeleteAsync(BlobRecord blobRecord, CancellationToken ct);
    }
}
