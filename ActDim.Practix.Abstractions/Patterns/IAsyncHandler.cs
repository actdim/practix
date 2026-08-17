using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for asynchronously handling a message, request, or event without returning a result.
    /// </summary>
    /// <typeparam name="TMessage">The type of message or event to handle.</typeparam>
    public interface IAsyncHandler<in TMessage>
    {
        /// <summary>
        /// Asynchronously handles the specified message.
        /// </summary>
        /// <param name="message">The incoming message or event.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous handle operation.</returns>
        Task HandleAsync(TMessage message, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously handling a request or query and producing a result.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to handle.</typeparam>
    /// <typeparam name="TResponse">The type of response produced.</typeparam>
    public interface IAsyncHandler<in TRequest, TResponse>
    {
        /// <summary>
        /// Asynchronously handles the specified request and returns a response.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous handle operation with the produced response.</returns>
        Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default);
    }
}
