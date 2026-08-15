using ActDim.Practix.Abstractions.Patterns;

namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Provider for obtaining the current ambient <see cref="IAmbientContext"/>.
    /// </summary>
    public interface IAmbientContextProvider : IProvider<IAmbientContext>
    {

    }
}
