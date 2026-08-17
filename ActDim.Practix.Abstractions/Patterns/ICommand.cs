namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for executing an encapsulated command or action without parameters.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Executes the command.
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// Contract for executing an encapsulated command or action accepting a parameter.
    /// </summary>
    /// <typeparam name="TArg">The parameter type.</typeparam>
    public interface ICommand<in TArg>
    {
        /// <summary>
        /// Executes the command with the specified parameter.
        /// </summary>
        /// <param name="arg">The command parameter.</param>
        void Execute(TArg arg);
    }

    /// <summary>
    /// Contract for executing an encapsulated command that returns a result.
    /// </summary>
    /// <typeparam name="TArg">The parameter type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface ICommand<in TArg, out TResult>
    {
        /// <summary>
        /// Executes the command with the specified parameter and returns a result.
        /// </summary>
        /// <param name="arg">The command parameter.</param>
        /// <returns>The execution result.</returns>
        TResult Execute(TArg arg);
    }
}
