namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for creating new instances of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    public interface IFactory<out TResult>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/>.
        /// </summary>
        /// <returns>A newly created instance.</returns>
        TResult Create();
    }

    /// <summary>
    /// Contract for creating new instances of type <typeparamref name="TResult"/> parameterized by an argument.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    /// <typeparam name="TArg">The input argument type.</typeparam>
    public interface IFactory<out TResult, in TArg>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified argument.
        /// </summary>
        /// <param name="arg">The parameter used to construct the instance.</param>
        /// <returns>A newly created instance.</returns>
        TResult Create(TArg arg);
    }

    /// <summary>
    /// Contract for creating new instances of type <typeparamref name="TResult"/> parameterized by two arguments.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    public interface IFactory<out TResult, in TArg1, in TArg2>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified arguments.
        /// </summary>
        /// <param name="arg1">The first parameter.</param>
        /// <param name="arg2">The second parameter.</param>
        /// <returns>A newly created instance.</returns>
        TResult Create(TArg1 arg1, TArg2 arg2);
    }

    /// <summary>
    /// Contract for creating new instances of type <typeparamref name="TResult"/> parameterized by three arguments.
    /// </summary>
    /// <typeparam name="TResult">The created product type.</typeparam>
    /// <typeparam name="TArg1">The first argument type.</typeparam>
    /// <typeparam name="TArg2">The second argument type.</typeparam>
    /// <typeparam name="TArg3">The third argument type.</typeparam>
    public interface IFactory<out TResult, in TArg1, in TArg2, in TArg3>
    {
        /// <summary>
        /// Creates a new instance of <typeparamref name="TResult"/> using the specified arguments.
        /// </summary>
        /// <param name="arg1">The first parameter.</param>
        /// <param name="arg2">The second parameter.</param>
        /// <param name="arg3">The third parameter.</param>
        /// <returns>A newly created instance.</returns>
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3);
    }
}
