namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract defining a business rule or predicate specification for entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The candidate entity type to evaluate.</typeparam>
    public interface ISpecification<in T>
    {
        /// <summary>
        /// Evaluates whether the specified candidate entity satisfies this specification.
        /// </summary>
        /// <param name="candidate">The candidate entity to evaluate.</param>
        /// <returns><c>true</c> if the candidate satisfies the specification; otherwise, <c>false</c>.</returns>
        bool IsSatisfiedBy(T candidate);
    }
}
