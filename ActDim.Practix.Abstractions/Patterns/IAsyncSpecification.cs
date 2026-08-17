using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns
{
    /// <summary>
    /// Contract defining an asynchronous business rule or predicate specification for entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The candidate entity type to evaluate.</typeparam>
    public interface IAsyncSpecification<in T>
    {
        /// <summary>
        /// Asynchronously evaluates whether the specified candidate entity satisfies this specification.
        /// </summary>
        /// <param name="candidate">The candidate entity to evaluate.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous evaluation with boolean result.</returns>
        Task<bool> IsSatisfiedByAsync(T candidate, CancellationToken ct = default);
    }
}
