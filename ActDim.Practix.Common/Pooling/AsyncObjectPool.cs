using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
    /// <remarks>
    /// Every permit acquired from the internal semaphore is released exactly once on every
    /// path (return, discard, factory failure, disposal), so before disposal the semaphore count
    /// never exceeds <c>maxSize</c>.
    /// The semaphore itself is never disposed: disposing a <see cref="SemaphoreSlim"/> does not
    /// complete pending <c>WaitAsync</c> calls, so waiters would hang forever. Instead
    /// <see cref="DisposeAsync"/> releases a single "baton" permit; each awakened waiter observes
    /// the disposed flag, passes the baton on by releasing one permit and throws
    /// <see cref="ObjectDisposedException"/>. This wakes every pending waiter without per-call
    /// <see cref="CancellationTokenSource"/> allocations or waiter counting, and late callers find
    /// the baton immediately available. A waiter cancelled via its token never consumed a permit,
    /// so the baton is never lost. The semaphore is created without a maximum count so that
    /// post-disposal releases can never throw <see cref="SemaphoreFullException"/>.
    /// Not disposing it is safe: <see cref="SemaphoreSlim"/> only owns an OS handle when
    /// <see cref="SemaphoreSlim.AvailableWaitHandle"/> is accessed, which this pool never does.
    /// </remarks>
    /// <typeparam name="T">The type of pooled objects.</typeparam>
    public sealed class AsyncObjectPool<T> : IAsyncDisposable where T : class
    {
        private readonly ConcurrentQueue<T> _items = new();
        private readonly SemaphoreSlim _semaphore;
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
            _semaphore = new SemaphoreSlim(maxSize);
        }

        /// <summary>
        /// Gets the maximum number of live instances the pool may hold.
        /// </summary>
        public int MaxSize => _maxSize;

        /// <summary>
        /// Gets the current number of created instances managed by the pool.
        /// </summary>
        public int CreatedCount => Volatile.Read(ref _createdCount);

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, typeof(AsyncObjectPool<T>));
        }

        /// <summary>
        /// Asynchronously acquires a pooled object wrapper from the pool.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="PooledObject"/> handle that returns the item to the pool upon disposal.</returns>
        public async Task<PooledObject> GetAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (Volatile.Read(ref _disposed) != 0)
            {
                // Pass the disposal baton on so the next pending waiter wakes up as well.
                _semaphore.Release();
                throw new ObjectDisposedException(nameof(AsyncObjectPool<T>));
            }

            if (_items.TryDequeue(out var item))
            {
                return new PooledObject(item, this);
            }

            try
            {
                item = await _factory().ConfigureAwait(false) ?? throw new InvalidOperationException("Factory returned null");

                if (Volatile.Read(ref _disposed) != 0)
                {
                    await DisposeItemAsync(item).ConfigureAwait(false);
                    throw new ObjectDisposedException(nameof(AsyncObjectPool<T>));
                }

                Interlocked.Increment(ref _createdCount);
                return new PooledObject(item, this);
            }
            catch
            {
                // Always release: before disposal it frees the slot, after disposal it passes the baton.
                _semaphore.Release();
                throw;
            }
        }

        /// <summary>
        /// Discards a corrupted or faulty object instead of returning it to the pool,
        /// invoking the configured disposer and freeing a capacity slot for new instances.
        /// </summary>
        /// <param name="item">The item to discard.</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        internal async ValueTask DiscardAsync(T item)
        {
            if (item == null)
            {
                return;
            }

            Interlocked.Decrement(ref _createdCount);
            _semaphore.Release();

            await DisposeItemAsync(item).ConfigureAwait(false);
        }

        private async ValueTask ReturnAsync(T item)
        {
            if (item == null)
            {
                return;
            }

            if (Volatile.Read(ref _disposed) != 0)
            {
                Interlocked.Decrement(ref _createdCount);
                _semaphore.Release();
                await DisposeItemAsync(item).ConfigureAwait(false);
                return;
            }

            _items.Enqueue(item);
            _semaphore.Release();

            // DisposeAsync may have drained the queue between the check above and the enqueue:
            // the disposed flag is published before the drain, so re-checking here guarantees
            // the item is disposed either by the drain or by this branch.
            if (Volatile.Read(ref _disposed) != 0 && _items.TryDequeue(out var queuedItem))
            {
                Interlocked.Decrement(ref _createdCount);
                await DisposeItemAsync(queuedItem).ConfigureAwait(false);
            }
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

            // Hand out the disposal baton first so pending waiters are not held up by slow disposers:
            // it wakes the first pending waiter, which passes it on.
            _semaphore.Release();

            List<Exception> exceptions = null;

            while (_items.TryDequeue(out var item))
            {
                Interlocked.Decrement(ref _createdCount);
                try
                {
                    await DisposeItemAsync(item).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException("One or more errors occurred while disposing pooled objects.", exceptions);
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
            internal PooledObject(T item, AsyncObjectPool<T> pool)
            {
                _item = item;
                _pool = pool;
            }

            /// <summary>
            /// Gets the leased pooled object item.
            /// </summary>
            public T Item => _item ?? throw new ObjectDisposedException(nameof(PooledObject));

            /// <summary>
            /// Discards the leased object from the pool due to fault or corruption instead
            /// of returning it for reuse, freeing its capacity slot in the pool.
            /// </summary>
            /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
            public ValueTask DiscardAsync()
            {
                var item = Interlocked.Exchange(ref _item, null);

                if (item == null)
                {
                    return ValueTask.CompletedTask;
                }

                return _pool.DiscardAsync(item);
            }

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
