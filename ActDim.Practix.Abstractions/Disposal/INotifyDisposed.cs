using System;

namespace ActDim.Practix.Abstractions.Disposal
{
    /// <summary>
    /// Contract for objects that expose their disposal state and notify subscribers when disposal occurs.
    /// Decoupled from <see cref="IDisposable"/> to support both synchronous and asynchronous disposal lifecycles.
    /// </summary>
    public interface INotifyDisposed : IDisposedState
    {
        /// <summary>
        /// Event raised when the object has been disposed.
        /// </summary>
        event EventHandler Disposed;
    }
}
