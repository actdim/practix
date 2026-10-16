namespace ActDim.Practix.Abstractions.Disposal
{
    /// <summary>
    /// Contract for objects that expose whether they have already been disposed.
    /// </summary>
    public interface IDisposedState
    {
        /// <summary>
        /// Gets a value indicating whether the object was already disposed.
        /// </summary>
        bool IsDisposed { get; }
    }
}
