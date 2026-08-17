using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for asynchronously providing an instance or value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The provided result type.</typeparam>
    public interface IAsyncProvider<TResult>
    {
        /// <summary>
        /// Asynchronously retrieves the value or instance.
        /// </summary>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the provided instance.</returns>
        Task<TResult> GetAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously providing an instance or value of type <typeparamref name="TResult"/> based on an argument.
    /// </summary>
    /// <typeparam name="TResult">The provided result type.</typeparam>
    /// <typeparam name="TArg">The input argument type.</typeparam>
    public interface IAsyncProvider<TResult, in TArg>
    {
        /// <summary>
        /// Asynchronously retrieves the value or instance using the supplied argument.
        /// </summary>
        /// <param name="arg">The argument used to resolve the instance.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the provided instance.</returns>
        Task<TResult> GetAsync(TArg arg, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously providing an instance or value of type <typeparamref name="TResult"/> based on two arguments.
    /// </summary>
    /// <typeparam name="TResult">The provided result type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    public interface IAsyncProvider<TResult, in TArg1, in TArg2>
    {
        /// <summary>
        /// Asynchronously retrieves the value or instance using the supplied arguments.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the provided instance.</returns>
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously providing an instance or value of type <typeparamref name="TResult"/> based on three arguments.
    /// </summary>
    /// <typeparam name="TResult">The provided result type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    /// <typeparam name="TArg3">The third argument type.</typeparam>
    public interface IAsyncProvider<TResult, in TArg1, in TArg2, in TArg3>
    {
        /// <summary>
        /// Asynchronously retrieves the value or instance using the supplied arguments.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the provided instance.</returns>
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously providing an instance or value of type <typeparamref name="TResult"/> based on four arguments.
    /// </summary>
    /// <typeparam name="TResult">The provided result type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    /// <typeparam name="TArg3">The third argument type.</typeparam>
    /// <typeparam name="TArg4">The fourth argument type.</typeparam>
    public interface IAsyncProvider<TResult, in TArg1, in TArg2, in TArg3, in TArg4>
    {
        /// <summary>
        /// Asynchronously retrieves the value or instance using the supplied arguments.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the provided instance.</returns>
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously providing an instance or value of type <typeparamref name="TResult"/> based on five arguments.
    /// </summary>
    /// <typeparam name="TResult">The provided result type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    /// <typeparam name="TArg3">The third argument type.</typeparam>
    /// <typeparam name="TArg4">The fourth argument type.</typeparam>
    /// <typeparam name="TArg5">The fifth argument type.</typeparam>
    public interface IAsyncProvider<TResult, in TArg1, in TArg2, in TArg3, in TArg4, in TArg5>
    {
        /// <summary>
        /// Asynchronously retrieves the value or instance using the supplied arguments.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation with the provided instance.</returns>
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken ct = default);
    }
}
