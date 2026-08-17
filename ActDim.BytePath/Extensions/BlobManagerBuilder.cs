using Microsoft.Extensions.DependencyInjection;
using System;

namespace ActDim.BytePath
{
    /// <summary>
    /// Default internal implementation of <see cref="IBlobManagerBuilder"/>.
    /// </summary>
    internal sealed class BlobManagerBuilder : IBlobManagerBuilder
    {
        public BlobManagerBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }
    }
}
