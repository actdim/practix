using Microsoft.Extensions.DependencyInjection;
using System;

namespace ActDim.BytePath.Extensions
{
    /// <summary>
    /// Default implementation of <see cref="IBlobManagerBuilder"/> used to configure blob storage backends in an <see cref="IServiceCollection"/>.
    /// </summary>
    public sealed class BlobManagerBuilder : IBlobManagerBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BlobManagerBuilder"/> with the target service collection.
        /// </summary>
        /// <param name="services">The service collection to register blob storage components into.</param>
        public BlobManagerBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
