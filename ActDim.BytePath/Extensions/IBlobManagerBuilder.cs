using Microsoft.Extensions.DependencyInjection;

namespace ActDim.BytePath
{
    /// <summary>
    /// Builder interface for configuring <see cref="IBlobManager"/> storage and registry backends in an <see cref="IServiceCollection"/>.
    /// </summary>
    public interface IBlobManagerBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where BlobManager services are configured.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
