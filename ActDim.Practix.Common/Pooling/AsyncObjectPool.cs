using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ActDim.Practix.Pooling
{
    /// <summary>
    /// Provides an asynchronous bounded object pool.
    /// 
    /// The pool reuses objects and limits the number of created instances.
    /// When no objects are available, callers asynchronously wait until an object
    /// is returned.
    /// 
    /// Returned objects are stored using FIFO ordering, providing predictable
    /// reuse behavior and reducing starvation during high contention.
    ///
    /// The pool <b>owns</b> the objects produced by <c>factory</c>: when supplied, the
    /// <c>disposer</c> is invoked for every idle object drained on <see cref="DisposeAsync"/>
    /// (e.g. when the pool is evicted from a cache) and for any object returned after the
    /// pool has been disposed. If no <c>disposer</c> is provided the pool does not touch the
    /// objects' lifecycle - ownership is an explicit, opt-in contract, never inferred.
    /// </summary>
    /// <typeparam name="T">The type of pooled objects.</typeparam>
    public sealed class AsyncObjectPool<T> : IAsyncDisposable where T : class
    {
        private readonly Channel<T> _channel;
        private readonly Func<Task<T>> _factory;
        private readonly Func<T, ValueTask> _disposer;
        private readonly int _maxSize;
        private int _createdCount;
        private int _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncObjectPool{T}"/> class.
        /// </summary>
        /// <param name="factory">Creates a new pooled instance when the pool grows.</param>
        /// <param name="maxSize">Maximum number of live instances the pool may hold.</param>
        /// <param name="disposer">
        /// Optional cleanup for objects the pool owns. Invoked on eviction/disposal and for
        /// objects returned to an already-disposed pool. When null, the pool never disposes.
        /// </param>
        public AsyncObjectPool(Func<Task<T>> factory, int maxSize, Func<T, ValueTask> disposer = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _disposer = disposer;
            _maxSize = maxSize;

            var options = new BoundedChannelOptions(maxSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };
            _channel = Channel.CreateBounded<T>(options);
        }

        /// <summary>
        /// Asynchronously acquires a pooled object wrapper from the pool.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="PooledObject"/> handle that returns the item to the pool upon disposal.</returns>
        public async Task<PooledObject> GetAsync(CancellationToken cancellationToken = default)
        {
            if (!_channel.Reader.TryRead(out var item))
            {
                var count = Interlocked.Increment(ref _createdCount);
                if (count <= _maxSize)
                {
                    try
                    {
                        item = await _factory() ?? throw new InvalidOperationException("Factory returned null");
                    }
                    catch
                    {
                        Interlocked.Decrement(ref _createdCount);
                        throw;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref _createdCount);
                    item = await _channel.Reader.ReadAsync(cancellationToken);
                }
            }

            return new PooledObject(item, this);
        }

        private ValueTask ReturnAsync(T item)
        {
            if (Volatile.Read(ref _disposed) != 0 || !_channel.Writer.TryWrite(item))
            {
                return DisposeItemAsync(item);
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Drains and disposes every idle object still parked in the pool. Invoked by the
        /// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> post-eviction callback when the pool's sliding
        /// expiration lapses, so idle instances do not stay resident indefinitely.
        /// </summary>
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _channel.Writer.TryComplete();

            while (_channel.Reader.TryRead(out var item))
            {
                await DisposeItemAsync(item);
            }
        }

        private ValueTask DisposeItemAsync(T item)
        {
            return _disposer?.Invoke(item) ?? ValueTask.CompletedTask;
        }

        /// <summary>
        /// A leased handle wrapping a pooled object of type <typeparamref name="T"/>.
        /// </summary>
        public sealed class PooledObject : IAsyncDisposable
        {
            private T _item;
            private readonly AsyncObjectPool<T> _pool;

            /// <summary>
            /// Initializes a new instance of the <see cref="PooledObject"/> class.
            /// </summary>
            /// <param name="item">The pooled instance.</param>
            /// <param name="pool">The owning object pool.</param>
            public PooledObject(T item, AsyncObjectPool<T> pool)
            {
                _item = item;
                _pool = pool;
            }

            /// <summary>
            /// Gets the leased pooled object item.
            /// </summary>
            public T Item => _item ?? throw new ObjectDisposedException(nameof(PooledObject));

            /// <inheritdoc />
            public ValueTask DisposeAsync()
            {
                var item = Interlocked.Exchange(ref _item, null);

                if (item == null)
                {
                    return ValueTask.CompletedTask;
                }

                return _pool.ReturnAsync(item);
            }
        }
    }
}
