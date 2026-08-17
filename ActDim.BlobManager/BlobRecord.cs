using System;
using System.Threading.Tasks;

namespace ActDim.BlobManager
{
    public class BlobRecord : IDisposable, IAsyncDisposable
    {
        public string Key { get; internal set; }

        public string Metadata { get; set; }
        public string ContentType { get; set; }

        /// <summary>
        /// Observed by the library from the data store, never declared by the caller.
        /// </summary>
        public long? Size { get; internal set; }

        /// <summary>
        /// Declared through <see cref="BlobStoreOptions.Hash"/>; the library is not going to accept a
        /// value written straight onto the record, because nothing would check it against the content.
        /// </summary>
        public string Hash { get; internal set; }

        public DateTimeOffset CreatedAt { get; internal set; }
        public DateTimeOffset UpdatedAt { get; internal set; }
        public DateTimeOffset AccessedAt { get; internal set; }

        public TimeSpan? SlidingExpiration { get; set; }
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
        /// The lock-free form, for the library's own use while a record is still being set up and its
        /// <see cref="LockType"/> is not decided yet. Takes <paramref name="now"/> so that every
        /// timestamp derived during one operation agrees.
        /// </summary>
        internal void Apply(BlobStoreOptions options, DateTimeOffset now)
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

        public Action OnDispose { get; internal set; }
        public Func<Task> OnDisposeAsync { get; internal set; }
        public LockType LockType { get; internal set; }

        private bool _isDisposed = false;

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
