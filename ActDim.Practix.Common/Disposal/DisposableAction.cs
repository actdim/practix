using System;

namespace ActDim.Practix.Disposal
{
    /// <summary>
    /// Executes an action when disposed.
    /// </summary>
    public sealed class DisposableAction : IDisposable
    {
        readonly Action _disposeAction;
        bool _disposed;

        public DisposableAction(Action disposeAction) // finalizer
        {
            _disposeAction = disposeAction;
        }

#pragma warning disable S125 // Sections of code should not be commented out
        // public DisposableAction(Delegate disposeAction) // finalizer
        // {
        // }
#pragma warning restore S125 // Sections of code should not be commented out

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_disposeAction != null)
                {
                    _disposeAction();
                }
                _disposed = true;
            }
        }
    }

    public sealed class DisposableBlock<T> : IDisposable
    {
        readonly Action<T> _disposeAction;
        /// <summary>
        /// Parameter
        /// </summary>
        public readonly T Data; // { get; private set; }

        bool _disposed;

        public DisposableBlock(Action<T> disposeAction, T data) // parameter
        {
            _disposeAction = disposeAction;
            Data = data;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_disposeAction != null)
                {
                    _disposeAction(Data);
                }
                _disposed = true;
            }
        }
    }
}