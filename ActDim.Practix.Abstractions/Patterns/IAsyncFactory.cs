using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for asynchronously creating new instances of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    public interface IAsyncFactory<TResult>
    {
        /// <summary>
        /// Asynchronously creates a new instance of <typeparamref name="TResult"/>.
        /// </summary>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the created instance.</returns>
        Task<TResult> CreateAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously creating new instances of type <typeparamref name="TResult"/> parameterized by an argument.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    /// <typeparam name="TArg">The input argument type.</typeparam>
    public interface IAsyncFactory<TResult, in TArg>
    {
        /// <summary>
        /// Asynchronously creates a new instance of <typeparamref name="TResult"/> using the specified argument.
        /// </summary>
        /// <param name="arg">The parameter used to construct the instance.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the created instance.</returns>
        Task<TResult> CreateAsync(TArg arg, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously creating new instances of type <typeparamref name="TResult"/> parameterized by two arguments.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    public interface IAsyncFactory<TResult, in TArg1, in TArg2>
    {
        /// <summary>
        /// Asynchronously creates a new instance of <typeparamref name="TResult"/> using the specified arguments.
        /// </summary>
        /// <param name="arg1">The first parameter.</param>
        /// <param name="arg2">The second parameter.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the created instance.</returns>
        Task<TResult> CreateAsync(TArg1 arg1, TArg2 arg2, CancellationToken ct = default);
    }
}
