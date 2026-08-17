namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract for incrementally building or constructing an instance of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The constructed product type.</typeparam>
    public interface IBuilder<out TResult>
    {
        /// <summary>
        /// Builds and returns the configured instance.
        /// </summary>
        /// <returns>The constructed instance of <typeparamref name="TResult"/>.</returns>
        TResult Build();
    }
}
