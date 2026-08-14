using ActDim.Practix.Abstractions.Patterns;

namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Provider for obtaining the current ambient <see cref="ICallContext"/>.
    /// </summary>
    public interface ICallContextProvider : IProvider<ICallContext>
    {

    }
}
