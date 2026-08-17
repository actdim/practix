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
    public sealed class DisposableAction : IDisposable
    {
        private Action _disposeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAction"/> class wrapping the specified disposal action.
        /// </summary>
        /// <param name="disposeAction">The action to execute on disposal.</param>
        public DisposableAction(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        /// <inheritdoc />
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
    /// <typeparam name="T">The parameter type.</typeparam>
    public sealed class DisposableAction<T> : IDisposable
    {
        private Action<T> _disposeAction;
        private T _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAction{T}"/> class with a disposal action and state parameter.
        /// </summary>
        /// <param name="disposeAction">The disposal action delegate.</param>
        /// <param name="data">The state parameter to pass to the disposal action.</param>
        public DisposableAction(Action<T> disposeAction, T data)
        {
            _disposeAction = disposeAction;
            _data = data;
        }

        /// <inheritdoc />
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAsyncAction"/> class wrapping the specified async disposal delegate.
        /// </summary>
        /// <param name="disposeAction">The async action to execute on disposal.</param>
        public DisposableAsyncAction(Func<ValueTask> disposeAction)
        {
            _disposeAction = disposeAction;
        }

        /// <inheritdoc />
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
    /// <typeparam name="T">The parameter type.</typeparam>
    public sealed class DisposableAsyncAction<T> : IAsyncDisposable
    {
        private Func<T, ValueTask> _disposeAction;
        private T _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAsyncAction{T}"/> class with an async disposal delegate and state parameter.
        /// </summary>
        /// <param name="disposeAction">The async disposal action delegate.</param>
        /// <param name="data">The state parameter to pass to the disposal action.</param>
        public DisposableAsyncAction(Func<T, ValueTask> disposeAction, T data)
        {
            _disposeAction = disposeAction;
            _data = data;
        }

        /// <inheritdoc />
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
