using System;
using System.Threading.Tasks;

namespace ActDim.Practix.BlobManager
{
    public class BlobRecord : IDisposable, IAsyncDisposable
    {
        public string Key { get; set; }
        public string Metadata { get; set; }
        public string ContentType { get; set; }
        public long? Size { get; set; }
        public string Hash { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset AccessedAt { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }

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
        }
    }
}
