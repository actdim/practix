using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.BlobManager
{
    /// <summary>
    /// High-level concurrency-aware blob management interface providing lock-based read/write access, expiration, and lifecycle operations.
    /// </summary>
    public interface IBlobManager
    {
        /// <summary>
        /// Gets the underlying physical data store instance.
        /// </summary>
        IBlobDataStore DataStore { get; }

        /// <summary>
        /// Tries to get an existing blob or create a new record if absent.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetOrSetAsync(string key, CancellationToken ct);

        /// <summary>
        /// Tries to get an existing blob or create a new record if absent, waiting up to <paramref name="timeout"/> for lock acquisition.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="timeout">The lock acquisition timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetOrSetAsync(string key, TimeSpan timeout, CancellationToken ct);

        /// <summary>
        /// Tries to get an existing blob or create a new record if absent with custom options and lock type.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="options">The store options for the record.</param>
        /// <param name="lockType">The requested lock type (Read or Write).</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, CancellationToken ct);

        /// <summary>
        /// Tries to get an existing blob or create a new record if absent with custom options, lock type, and lock acquisition timeout.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="options">The store options for the record.</param>
        /// <param name="lockType">The requested lock type (Read or Write).</param>
        /// <param name="timeout">The lock acquisition timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetOrSetAsync(string key, BlobStoreOptions options, LockType lockType, TimeSpan timeout, CancellationToken ct);

        /// <summary>
        /// Tries to acquire a shared read lock for an existing blob.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetForReadingAsync(string key, CancellationToken ct);

        /// <summary>
        /// Tries to acquire a shared read lock for an existing blob, waiting up to <paramref name="timeout"/>.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="timeout">The lock acquisition timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetForReadingAsync(string key, TimeSpan timeout, CancellationToken ct);

        /// <summary>
        /// Tries to acquire an exclusive write lock for an existing blob.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetForWritingAsync(string key, CancellationToken ct);

        /// <summary>
        /// Tries to acquire an exclusive write lock for an existing blob, waiting up to <paramref name="timeout"/>.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="timeout">The lock acquisition timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetForWritingAsync(string key, TimeSpan timeout, CancellationToken ct);

        /// <summary>
        /// Acquires the write lock on an existing blob and applies <paramref name="options"/> to the
        /// record it hands out — the same thing as calling <see cref="BlobRecord.Apply"/> on the
        /// result, spelled as one call. The values reach storage when the record is disposed, so a
        /// caller that never disposes the handle persists nothing, exactly as with any other
        /// mutation made through it.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="options">The store options to apply.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, CancellationToken ct);

        /// <summary>
        /// Acquires the write lock on an existing blob and applies <paramref name="options"/> to the
        /// record it hands out, waiting up to <paramref name="timeout"/> — the same thing as calling
        /// <see cref="BlobRecord.Apply"/> on the result, spelled as one call.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="options">The store options to apply.</param>
        /// <param name="timeout">The lock acquisition timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="BlobResult"/> indicating success or error details.</returns>
        Task<BlobResult> TryGetForWritingAsync(string key, BlobStoreOptions options, TimeSpan timeout, CancellationToken ct);

        /// <summary>
        /// Queries stored blob keys matching the specified wildcard <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">The pattern string (e.g. "users/*").</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A list of matching blob keys.</returns>
        Task<IList<string>> QueryAsync(string pattern, CancellationToken ct);

        /// <summary>
        /// Deletes the specified blob and its metadata after acquiring an exclusive write lock.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="ct">The cancellation token.</param>
        Task DeleteAsync(string key, CancellationToken ct);

        /// <summary>
        /// Deletes the specified blob and its metadata after acquiring an exclusive write lock, waiting up to <paramref name="timeout"/>.
        /// </summary>
        /// <param name="key">The unique blob key.</param>
        /// <param name="timeout">The lock acquisition timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        Task DeleteAsync(string key, TimeSpan timeout, CancellationToken ct);

        /// <summary>
        /// Deletes all blobs whose expiration time has passed.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The number of deleted blobs.</returns>
        Task<int> DeleteExpiredAsync(CancellationToken ct);

        /// <summary>
        /// Deletes blobs older than the specified <paramref name="cutoff"/> date.
        /// </summary>
        /// <param name="cutoff">The cutoff timestamp.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <param name="forceDeleteLocked">Whether to force deletion even if currently locked.</param>
        /// <returns>The number of deleted blobs.</returns>
        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct, bool forceDeleteLocked = false);

        /// <summary>
        /// Performs housekeeping tasks such as purging stale locks and orphaned metadata.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        Task CleanupAsync(CancellationToken ct);
    }
}
