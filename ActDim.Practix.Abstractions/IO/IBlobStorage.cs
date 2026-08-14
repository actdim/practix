using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.IO
{
    /// <summary>
    /// Contract for blob storage operations, providing async access for retrieving, searching, writing, and deleting binary objects.
    /// </summary>
    public interface IBlobStorage
    {
        /// <summary>
        /// Retrieves a blob by its unique key.
        /// </summary>
        /// <param name="key">The unique key identifying the blob.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        /// <returns>A task representing the async operation, containing the requested <see cref="IBlob"/>.</returns>
        Task<IBlob> GetAsync(string key, CancellationToken ct);

        /// <summary>
        /// Finds all blobs matching a specified pattern (e.g. GLOB or regular expression).
        /// </summary>
        /// <param name="pattern">The pattern used to filter blob keys.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        /// <returns>A task containing the list of matching <see cref="IBlob"/> instances.</returns>
        Task<IList<IBlob>> FindAsync(string pattern, CancellationToken ct);

        /// <summary>
        /// Saves a stream data blob under the specified key and options.
        /// </summary>
        /// <param name="key">The unique key identifying the target blob.</param>
        /// <param name="data">The stream containing the blob content.</param>
        /// <param name="options">Options controlling storage options such as tags or expiration.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        Task SaveAsync(string key, Stream data, IStorageOptions options, CancellationToken ct);

        /// <summary>
        /// Saves a byte memory blob under the specified key and options.
        /// </summary>
        /// <param name="key">The unique key identifying the target blob.</param>
        /// <param name="data">The read-only byte memory payload.</param>
        /// <param name="options">Options controlling storage options such as tags or expiration.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        Task SaveAsync(string key, ReadOnlyMemory<byte> data, IStorageOptions options, CancellationToken ct);

        /// <summary>
        /// Deletes a blob by its unique key.
        /// </summary>
        /// <param name="key">The unique key of the blob to delete.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        Task DeleteAsync(string key, CancellationToken ct);

        /// <summary>
        /// Deletes all blobs that have expired based on their storage options.
        /// </summary>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        /// <returns>The total number of deleted expired blobs.</returns>
        Task<int> DeleteExpiredAsync(CancellationToken ct);

        /// <summary>
        /// Deletes all blobs that were created or modified before the specified cutoff date.
        /// </summary>
        /// <param name="cutoff">The threshold date before which blobs will be removed.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        /// <returns>The total number of deleted blobs.</returns>
        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);
    }
}
