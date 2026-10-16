using System;
using System.Threading.Tasks;
using ActDim.Practix.Abstractions.Disposal;
using ActDim.Practix.Disposal;
using Xunit;

namespace ActDim.Practix.Common.Tests.Disposal
{
    public class DisposalTests
    {
        [Fact]
        public void DisposableAction_DisposesOnce_AndUpdatesIsDisposed()
        {
            var callCount = 0;
            var disposable = new DisposableAction(() =>
            {
                callCount++;
            });

            Assert.False(disposable.IsDisposed);
            Assert.Equal(0, callCount);

            disposable.Dispose();

            Assert.True(disposable.IsDisposed);
            Assert.Equal(1, callCount);

            disposable.Dispose();

            Assert.True(disposable.IsDisposed);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void DisposableActionOfT_PassesState_AndUpdatesIsDisposed()
        {
            string captured = null;
            var disposable = new DisposableAction<string>(state =>
            {
                captured = state;
            }, "test-payload");

            Assert.False(disposable.IsDisposed);
            Assert.Null(captured);

            disposable.Dispose();

            Assert.True(disposable.IsDisposed);
            Assert.Equal("test-payload", captured);

            disposable.Dispose();

            Assert.True(disposable.IsDisposed);
            Assert.Equal("test-payload", captured);
        }

        [Fact]
        public async Task DisposableAsyncAction_DisposesOnce_AndUpdatesIsDisposed()
        {
            var callCount = 0;
            var disposable = new DisposableAsyncAction(() =>
            {
                callCount++;
                return ValueTask.CompletedTask;
            });

            Assert.False(disposable.IsDisposed);
            Assert.Equal(0, callCount);

            await disposable.DisposeAsync();

            Assert.True(disposable.IsDisposed);
            Assert.Equal(1, callCount);

            await disposable.DisposeAsync();

            Assert.True(disposable.IsDisposed);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task DisposableAsyncActionOfT_PassesState_AndUpdatesIsDisposed()
        {
            int captured = 0;
            var disposable = new DisposableAsyncAction<int>(state =>
            {
                captured = state;
                return ValueTask.CompletedTask;
            }, 42);

            Assert.False(disposable.IsDisposed);
            Assert.Equal(0, captured);

            await disposable.DisposeAsync();

            Assert.True(disposable.IsDisposed);
            Assert.Equal(42, captured);

            await disposable.DisposeAsync();

            Assert.True(disposable.IsDisposed);
            Assert.Equal(42, captured);
        }

        [Fact]
        public void ExceptionAwareDisposable_CustomImplementation_TracksExceptionAndDisposedState()
        {
            var instance = new TestExceptionAwareDisposable();
            var expectedException = new InvalidOperationException("Disposal due to failure");

            Assert.False(instance.IsDisposed);
            Assert.Null(instance.DisposeException);

            instance.Dispose(expectedException);

            Assert.True(instance.IsDisposed);
            Assert.Same(expectedException, instance.DisposeException);

            // Repeated dispose with another exception should preserve the first
            instance.Dispose(new Exception("Secondary"));
            Assert.Same(expectedException, instance.DisposeException);
        }

        [Fact]
        public void NotifyDisposed_SynchronousResource_RaisesDisposedEvent()
        {
            var instance = new TestNotifyDisposed();
            var eventFired = false;
            object senderArg = null;

            instance.Disposed += (sender, args) =>
            {
                eventFired = true;
                senderArg = sender;
            };

            Assert.False(instance.IsDisposed);
            Assert.False(eventFired);

            instance.Dispose();

            Assert.True(instance.IsDisposed);
            Assert.True(eventFired);
            Assert.Same(instance, senderArg);
        }

        [Fact]
        public async Task NotifyDisposed_AsynchronousResource_RaisesDisposedEvent()
        {
            var instance = new TestAsyncNotifyDisposed();
            var eventFired = false;
            object senderArg = null;

            instance.Disposed += (sender, args) =>
            {
                eventFired = true;
                senderArg = sender;
            };

            Assert.False(instance.IsDisposed);
            Assert.False(eventFired);

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
            Assert.True(eventFired);
            Assert.Same(instance, senderArg);
        }

        private sealed class TestExceptionAwareDisposable : IExceptionAwareDisposable
        {
            public bool IsDisposed { get; private set; }
            public Exception DisposeException { get; private set; }

            public void Dispose()
            {
                Dispose(null);
            }

            public void Dispose(Exception exception)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    DisposeException = exception;
                }
            }
        }

        private sealed class TestNotifyDisposed : IDisposable, INotifyDisposed
        {
            public bool IsDisposed { get; private set; }

            public event EventHandler Disposed;

            public void Dispose()
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    Disposed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private sealed class TestAsyncNotifyDisposed : IAsyncDisposable, INotifyDisposed
        {
            public bool IsDisposed { get; private set; }

            public event EventHandler Disposed;

            public ValueTask DisposeAsync()
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    Disposed?.Invoke(this, EventArgs.Empty);
                }

                return ValueTask.CompletedTask;
            }
        }
    }
}
