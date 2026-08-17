using System;

namespace ActDim.Practix.Common.Introspection
{
    /// <summary>
    /// Attribute specifying the provider type responsible for generating custom <see cref="IntrospectionInfo"/> metadata.
    /// </summary>
    public class IntrospectionInfoProviderAttribute : Attribute
    {
        /// <summary>
        /// Gets the provider type implementation.
        /// </summary>
        public Type ProviderType { get; init; }
    }
}
