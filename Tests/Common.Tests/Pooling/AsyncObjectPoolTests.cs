using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ActDim.Practix.Pooling;
using Xunit;

namespace ActDim.Practix.Common.Tests.Pooling
{
    public class AsyncObjectPoolTests
    {
        private sealed class TestResource
        {
            public int Id { get; init; }
            public bool IsFaulted { get; set; }
        }

        [Fact]
        public async Task GetAsync_ReusesReturnedObject_WhenDisposedNormally()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: 2);

            TestResource firstItem;
            await using (var pooled = await pool.GetAsync(ct))
            {
                firstItem = pooled.Item;
                Assert.Equal(1, firstItem.Id);
                Assert.Equal(1, pool.CreatedCount);
            }

            await using (var pooled = await pool.GetAsync(ct))
            {
                Assert.Same(firstItem, pooled.Item);
                Assert.Equal(1, pool.CreatedCount);
            }
        }

        [Fact]
        public async Task DiscardAsync_InvokesDisposerAndDoesNotReturnToPool()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedItems = new ConcurrentBag<TestResource>();

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: 2,
                disposer: item =>
                {
                    disposedItems.Add(item);
                    return ValueTask.CompletedTask;
                });

            TestResource discardedResource;
            var pooled = await pool.GetAsync(ct);
            discardedResource = pooled.Item;
            discardedResource.IsFaulted = true;

            await pooled.DiscardAsync();

            Assert.Single(disposedItems);
            Assert.Contains(discardedResource, disposedItems);
            Assert.Equal(0, pool.CreatedCount);

            // Next acquire must create a fresh object
            await using (var nextPooled = await pool.GetAsync(ct))
            {
                Assert.NotSame(discardedResource, nextPooled.Item);
                Assert.Equal(2, nextPooled.Item.Id);
                Assert.Equal(1, pool.CreatedCount);
            }
        }

        [Fact]
        public async Task DiscardAsync_PreventsSlotStarvation_AcrossRepeatedFailures()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedCount = 0;
            const int maxSize = 2;
            const int iterations = 10;

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: maxSize,
                disposer: _ =>
                {
                    Interlocked.Increment(ref disposedCount);
                    return ValueTask.CompletedTask;
                });

            for (var i = 0; i < iterations; i++)
            {
                var pooled = await pool.GetAsync(ct);
                Assert.NotNull(pooled.Item);
                await pooled.DiscardAsync();
                Assert.Equal(0, pool.CreatedCount);
            }

            Assert.Equal(iterations, createdId);
            Assert.Equal(iterations, disposedCount);
        }

        [Fact]
        public async Task DiscardAsync_ThenDisposeAsync_IsIdempotent()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedCount = 0;

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: 2,
                disposer: _ =>
                {
                    Interlocked.Increment(ref disposedCount);
                    return ValueTask.CompletedTask;
                });

            var pooled = await pool.GetAsync(ct);
            Assert.Equal(1, pool.CreatedCount);

            // Discard explicitly
            await pooled.DiscardAsync();
            Assert.Equal(0, pool.CreatedCount);
            Assert.Equal(1, disposedCount);

            // Subsequent DisposeAsync (as would happen with await using)
            await pooled.DisposeAsync();
            Assert.Equal(0, pool.CreatedCount);
            Assert.Equal(1, disposedCount);

            // Item property throws ObjectDisposedException
            Assert.Throws<ObjectDisposedException>(() => pooled.Item);
        }

        [Fact]
        public async Task DisposeAsync_ThenDiscardAsync_IsIdempotent()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedCount = 0;

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: 2,
                disposer: _ =>
                {
                    Interlocked.Increment(ref disposedCount);
                    return ValueTask.CompletedTask;
                });

            var pooled = await pool.GetAsync(ct);
            Assert.Equal(1, pool.CreatedCount);

            // Dispose first (returns to pool)
            await pooled.DisposeAsync();
            Assert.Equal(1, pool.CreatedCount);
            Assert.Equal(0, disposedCount);

            // Discard after disposal is a no-op on already returned handle
            await pooled.DiscardAsync();
            Assert.Equal(1, pool.CreatedCount);
            Assert.Equal(0, disposedCount);
        }

        [Fact]
        public async Task DirectDiscardAsync_HandlesNullAndValidItem()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedCount = 0;

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: 2,
                disposer: _ =>
                {
                    Interlocked.Increment(ref disposedCount);
                    return ValueTask.CompletedTask;
                });

            // Discard null does not throw or decrement
            await pool.DiscardAsync(null);
            Assert.Equal(0, pool.CreatedCount);

            var pooled = await pool.GetAsync(ct);
            Assert.Equal(1, pool.CreatedCount);
            var item = pooled.Item;

            await pool.DiscardAsync(item);
            Assert.Equal(0, pool.CreatedCount);
            Assert.Equal(1, disposedCount);
        }

        [Fact]
        public async Task Concurrent_LeaseAndDiscard_UnderLoad()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedCount = 0;
            const int maxSize = 5;
            const int taskCount = 30;

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: maxSize,
                disposer: _ =>
                {
                    Interlocked.Increment(ref disposedCount);
                    return ValueTask.CompletedTask;
                });

            var tasks = new Task[taskCount];
            for (var i = 0; i < taskCount; i++)
            {
                var index = i;
                tasks[i] = Task.Run(async () =>
                {
                    var pooled = await pool.GetAsync(ct);
                    await Task.Delay(5, ct);

                    if (index % 3 == 0)
                    {
                        await pooled.DiscardAsync();
                    }
                    else
                    {
                        await pooled.DisposeAsync();
                    }
                }, ct);
            }

            await Task.WhenAll(tasks);
            Assert.InRange(pool.CreatedCount, 0, maxSize);

            // Clean up pool
            await pool.DisposeAsync();
        }

        [Fact]
        public async Task DisposeAsync_DrainsAllIdleObjects_EvenWhenDisposerThrows_AndThrowsAggregateException()
        {
            var ct = TestContext.Current.CancellationToken;
            var createdId = 0;
            var disposedIds = new ConcurrentBag<int>();

            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = Interlocked.Increment(ref createdId) }),
                maxSize: 3,
                disposer: item =>
                {
                    disposedIds.Add(item.Id);
                    if (item.Id == 2)
                    {
                        throw new InvalidOperationException("Disposal failure for item 2");
                    }

                    return ValueTask.CompletedTask;
                });

            // Acquire 3 items
            var p1 = await pool.GetAsync(ct);
            var p2 = await pool.GetAsync(ct);
            var p3 = await pool.GetAsync(ct);

            Assert.Equal(3, pool.CreatedCount);

            // Return all 3 items to pool
            await p1.DisposeAsync();
            await p2.DisposeAsync();
            await p3.DisposeAsync();

            Assert.Equal(3, pool.CreatedCount);

            // Dispose pool: should drain all 3 items, record all 3, and throw AggregateException
            var aggregateEx = await Assert.ThrowsAsync<AggregateException>(async () => await pool.DisposeAsync());
            Assert.Single(aggregateEx.InnerExceptions);
            Assert.IsType<InvalidOperationException>(aggregateEx.InnerExceptions[0]);
            Assert.Equal("Disposal failure for item 2", aggregateEx.InnerExceptions[0].Message);

            // Verify all items were dequeued and processed
            Assert.Contains(1, disposedIds);
            Assert.Contains(2, disposedIds);
            Assert.Contains(3, disposedIds);
            Assert.Equal(3, disposedIds.Count);
            Assert.Equal(0, pool.CreatedCount);
        }

        [Fact]
        public async Task GetAsync_ThrowsObjectDisposedException_WhenPoolIsDisposed()
        {
            var ct = TestContext.Current.CancellationToken;
            var pool = new AsyncObjectPool<TestResource>(
                () => Task.FromResult(new TestResource { Id = 1 }),
                maxSize: 2);

            await pool.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pool.GetAsync(ct));
        }

        [Fact]
        public async Task GetAsync_CleansUpFactoryCreatedItem_WhenPoolDisposedDuringFactoryExecution()
        {
            var ct = TestContext.Current.CancellationToken;
            var factoryStartedTcs = new TaskCompletionSource<bool>();
            var factoryContinueTcs = new TaskCompletionSource<bool>();
            var disposedItems = new ConcurrentBag<TestResource>();

            var pool = new AsyncObjectPool<TestResource>(
                async () =>
                {
                    factoryStartedTcs.TrySetResult(true);
                    await factoryContinueTcs.Task;
                    return new TestResource { Id = 42 };
                },
                maxSize: 2,
                disposer: item =>
                {
                    disposedItems.Add(item);
                    return ValueTask.CompletedTask;
                });

            // Start GetAsync
            var getTask = Task.Run(async () => await pool.GetAsync(ct), ct);

            // Wait until factory has started
            await factoryStartedTcs.Task;

            // Dispose pool while factory is paused
            var disposeTask = Task.Run(async () => await pool.DisposeAsync(), ct);

            // Let factory finish creating the object
            factoryContinueTcs.TrySetResult(true);

            // GetAsync must throw ObjectDisposedException
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await getTask);
            await disposeTask;

            // The created item must have been immediately cleaned up via disposer
            Assert.Single(disposedItems);
            Assert.Equal(42, disposedItems.ToArray()[0].Id);
            Assert.Equal(0, pool.CreatedCount);
        }
    }
}

