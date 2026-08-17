using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for asynchronously executing an encapsulated command or action without parameters.
    /// </summary>
    public interface IAsyncCommand
    {
        /// <summary>
        /// Asynchronously executes the command.
        /// </summary>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous command execution.</returns>
        Task ExecuteAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously executing an encapsulated command or action accepting a parameter.
    /// </summary>
    /// <typeparam name="TArg">The parameter type.</typeparam>
    public interface IAsyncCommand<in TArg>
    {
        /// <summary>
        /// Asynchronously executes the command with the specified parameter.
        /// </summary>
        /// <param name="arg">The command parameter.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous command execution.</returns>
        Task ExecuteAsync(TArg arg, CancellationToken ct = default);
    }

    /// <summary>
    /// Contract for asynchronously executing an encapsulated command that returns a result.
    /// </summary>
    /// <typeparam name="TArg">The parameter type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface IAsyncCommand<in TArg, TResult>
    {
        /// <summary>
        /// Asynchronously executes the command with the specified parameter and returns a result.
        /// </summary>
        /// <param name="arg">The command parameter.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous command execution with the result.</returns>
        Task<TResult> ExecuteAsync(TArg arg, CancellationToken ct = default);
    }
}
