using System;
using System.Threading.Tasks;

namespace ActDim.BytePath
{
    /// <summary>
    /// Represents an active handle to a blob's metadata and content size under a concurrency lock.
    /// </summary>
    public class BlobRecord : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets or sets the unique string key identifying this blob.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets custom text metadata stored alongside the blob.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the MIME content type of the blob.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the observed payload size in bytes.
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets the content hash or checksum.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the blob record was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the blob record was last updated.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the blob record was last accessed.
        /// </summary>
        public DateTimeOffset AccessedAt { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration window duration.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the absolute expiration timestamp.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Applies <paramref name="options"/> to this record. Where a direct assignment sets state,
        /// options carry instructions: <see cref="BlobStoreOptions.Ttl"/> is relative and is resolved
        /// against the current time, only the values that are set are applied, and expiration follows
        /// the priority AbsoluteExpiration &gt; Ttl &gt; existing SlidingExpiration.
        /// </summary>
        /// <remarks>
        /// The changes are persisted when the record is disposed, so this requires the write lock the
        /// record was handed out with.
        /// </remarks>
        public void Apply(BlobStoreOptions options)
        {
            if (LockType != LockType.Write)
            {
                throw new InvalidOperationException("Applying options requires a write lock on the blob record.");
            }

            Apply(options, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Applies options with an explicit timestamp reference for consistency.
        /// </summary>
        public void Apply(BlobStoreOptions options, DateTimeOffset now)
        {
            if (options == null)
            {
                // Still an access: refresh a sliding expiry even when nothing was requested.
                if (SlidingExpiration.HasValue)
                {
                    ExpiresAt = now.Add(SlidingExpiration.Value);
                }

                return;
            }

            if (!string.IsNullOrEmpty(options.ContentType))
            {
                ContentType = options.ContentType;
            }

            if (!string.IsNullOrEmpty(options.Hash))
            {
                Hash = options.Hash;
            }

            if (!string.IsNullOrEmpty(options.Metadata))
            {
                Metadata = options.Metadata;
            }

            if (options.AbsoluteExpiration.HasValue)
            {
                ExpiresAt = options.AbsoluteExpiration.Value;
            }
            else if (options.Ttl.HasValue)
            {
                ExpiresAt = now.Add(options.Ttl.Value);
            }
            else if (SlidingExpiration.HasValue)
            {
                ExpiresAt = now.Add(SlidingExpiration.Value);
            }

            if (options.SlidingExpiration.HasValue)
            {
                SlidingExpiration = options.SlidingExpiration;
                ExpiresAt = now.Add(options.SlidingExpiration.Value);
            }
        }

        /// <summary>
        /// Gets or sets the synchronous disposal callback invoked when the record lock is released.
        /// </summary>
        public Action OnDispose { get; set; }

        /// <summary>
        /// Gets or sets the asynchronous disposal callback invoked when the record lock is released.
        /// </summary>
        public Func<Task> OnDisposeAsync { get; set; }

        /// <summary>
        /// Gets or sets the current lock type held on this blob record.
        /// </summary>
        public LockType LockType { get; set; }

        private bool _isDisposed = false;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (OnDispose != null)
            {
                OnDispose();
            }
            else
            {
                OnDisposeAsync?.Invoke().GetAwaiter().GetResult();
            }

            LockType = LockType.None;

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (OnDisposeAsync != null)
            {
                await OnDisposeAsync();
            }
            else
            {
                OnDispose?.Invoke();
            }

            LockType = LockType.None;

            GC.SuppressFinalize(this);
        }
    }
}
