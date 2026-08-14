#nullable enable
using System;
using System.Collections.Generic;

namespace ActDim.Practix.Observability
{
    /// <summary>
    /// Configuration options for <see cref="EventObservabilityBridge"/>.
    /// </summary>
    public class EventObservabilityOptions
    {
        /// <summary>
        /// Gets or sets whether external scopes (from Microsoft.Extensions.Logging.IExternalScopeProvider) are written into telemetry tags.
        /// Default is <c>true</c>. Can be suppressed dynamically per async scope via <c>callContext.SuppressExternalScopes()</c>.
        /// </summary>
        public bool IncludeExternalScopes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether ambient properties from ICallContextProvider are written into telemetry tags.
        /// Default is <c>true</c>. Can be suppressed dynamically per async scope via <c>callContext.SuppressCallContext()</c>.
        /// </summary>
        public bool IncludeCallContext { get; set; } = true;

        /// <summary>
        /// Custom mapping of logger provider types to custom alias names.
        /// </summary>
        public Dictionary<Type, string> CustomProviderAliases { get; } = [];

        /// <summary>
        /// Registers a custom provider alias for a specific logger provider type.
        /// </summary>
        public EventObservabilityOptions RegisterProviderAlias<TProvider>(string alias)
        {
            return RegisterProviderAlias(typeof(TProvider), alias);
        }

        /// <summary>
        /// Registers a custom provider alias for a specific logger provider type.
        /// </summary>
        public EventObservabilityOptions RegisterProviderAlias(Type providerType, string alias)
        {
            if (providerType == null)
            {
                throw new ArgumentNullException(nameof(providerType));
            }

            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new ArgumentException("Alias cannot be null or whitespace.", nameof(alias));
            }

            CustomProviderAliases[providerType] = alias;
            return this;
        }
    }
}
