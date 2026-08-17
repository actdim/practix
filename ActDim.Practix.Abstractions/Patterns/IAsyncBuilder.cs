using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for asynchronously building or constructing an instance of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The constructed product type.</typeparam>
    public interface IAsyncBuilder<TResult>
    {
        /// <summary>
        /// Asynchronously builds and returns the configured instance.
        /// </summary>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous build operation with the constructed instance.</returns>
        Task<TResult> BuildAsync(CancellationToken ct = default);
    }
}
