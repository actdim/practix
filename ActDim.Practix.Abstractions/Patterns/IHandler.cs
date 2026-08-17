namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for handling a message, request, or event without returning a result.
    /// </summary>
    /// <typeparam name="TMessage">The type of message or event to handle.</typeparam>
    public interface IHandler<in TMessage>
    {
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The incoming message or event.</param>
        void Handle(TMessage message);
    }

    /// <summary>
    /// Contract for handling a request or query and producing a result.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to handle.</typeparam>
    /// <typeparam name="TResponse">The type of response produced.</typeparam>
    public interface IHandler<in TRequest, out TResponse>
    {
        /// <summary>
        /// Handles the specified request and returns a response.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>The response produced by handling the request.</returns>
        TResponse Handle(TRequest request);
    }
}
