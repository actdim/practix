using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Disposal
{
    /// <summary>
    /// Executes an action exactly once when disposed. Disposal is atomic and idempotent: concurrent or
    /// repeated <see cref="Dispose"/> calls run the action at most once, and the captured delegate is
    /// released afterwards.
    /// </summary>
    /// <inheritdoc />
    public sealed class DisposableAction : IDisposable
    {
        private Action _disposeAction;

        public DisposableAction(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _disposeAction, null);
            action?.Invoke();
        }
    }

    /// <summary>
    /// Same as <see cref="DisposableAction"/>, but carries a parameter that the action receives on disposal.
    /// Lets callers use a cached (non-capturing) delegate plus state instead of an allocating closure. Both
    /// the delegate and the captured state are released once disposed, so nothing is retained.
    /// </summary>
    public sealed class DisposableAction<T> : IDisposable
    {
        private Action<T> _disposeAction;

        private T _data;

        public DisposableAction(Action<T> disposeAction, T data)
        {
            _disposeAction = disposeAction;
            _data = data;
        }

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _disposeAction, null);
            if (action != null)
            {
                action(_data);
                _data = default;
            }
        }
    }

    /// <summary>
    /// Asynchronous counterpart of <see cref="DisposableAction"/>: runs an async action exactly once when
    /// disposed. Disposal is atomic and idempotent.
    /// </summary>
    public sealed class DisposableAsyncAction : IAsyncDisposable
    {
        private Func<ValueTask> _disposeAction;

        public DisposableAsyncAction(Func<ValueTask> disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public ValueTask DisposeAsync()
        {
            var action = Interlocked.Exchange(ref _disposeAction, null);
            return action?.Invoke() ?? ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Asynchronous counterpart of <see cref="DisposableAction{T}"/>: runs an async action with a carried
    /// parameter exactly once when disposed, releasing both the delegate and the captured state afterwards.
    /// </summary>
    public sealed class DisposableAsyncAction<T> : IAsyncDisposable
    {
        private Func<T, ValueTask> _disposeAction;

        private T _data;

        public DisposableAsyncAction(Func<T, ValueTask> disposeAction, T data)
        {
            _disposeAction = disposeAction;
            _data = data;
        }

        public ValueTask DisposeAsync()
        {
            var action = Interlocked.Exchange(ref _disposeAction, null);
            if (action == null)
            {
                return ValueTask.CompletedTask;
            }

            var task = action(_data);
            _data = default;
            return task;
        }
    }
}
