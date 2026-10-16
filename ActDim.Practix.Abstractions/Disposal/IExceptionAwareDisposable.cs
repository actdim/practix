using System;

namespace ActDim.Practix.Abstractions.Disposal
{
    /// <summary>
    /// Contract for disposable objects capable of capturing and exposing the exception that caused disposal.
    /// </summary>
    public interface IExceptionAwareDisposable : IDisposable, IDisposedState
    {
        /// <summary>
        /// Gets the exception that caused the disposal of this object, or <see langword="null"/> if disposed normally.
        /// </summary>
        Exception? DisposeException { get; }

        /// <summary>
        /// Disposes the object, capturing the exception that triggered its disposal.
        /// </summary>
        /// <param name="exception">The exception responsible for triggering disposal, or <see langword="null"/>.</param>
        void Dispose(Exception? exception);
    }
}
