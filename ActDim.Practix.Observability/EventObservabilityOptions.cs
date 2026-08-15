#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace ActDim.Practix.Observability
{
    /// <summary>
    /// Configuration options for <see cref="EventObservabilityBridge"/>.
    /// </summary>
    public class EventObservabilityOptions
    {
        /// <summary>
        /// Gets or sets whether an <see cref="Activity"/> is automatically started on BeginScope if <see cref="Activity.Current"/> is null.
        /// Default is <c>true</c>.
        /// </summary>
        public bool AutoCreateActivityOnScope { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum recursion depth when flattening objects into OpenTelemetry attributes.
        /// Default is <c>3</c>.
        /// </summary>
        public int MaxFlattenDepth { get; set; } = 3;

        /// <summary>
        /// Gets or sets the maximum number of attributes generated when flattening an object into OpenTelemetry attributes.
        /// Default is <c>100</c>.
        /// </summary>
        public int MaxFlattenAttributes { get; set; } = 100;

        /// <summary>
        /// Gets or sets the default ActivitySource name used when no custom source is specified in the ambient context.
        /// Defaults to EntryAssembly name or "ActDim.Practix".
        /// </summary>
        public string DefaultActivitySourceName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name ?? "ActDim.Practix";

        /// <summary>
        /// Gets or sets whether external scopes (from <see cref="Microsoft.Extensions.Logging.IExternalScopeProvider"/>) are written
        /// into <see cref="Activity"/> tags. Default is <c>false</c>. Can be suppressed dynamically per async scope via
        /// <see cref="IObservabilityContext.SuppressExternalScopes"/>.
        /// </summary>
        public bool IncludeExternalScopes { get; set; } = false;

        /// <summary>
        /// Gets or sets whether a logged <see cref="Exception"/> is reported to the current <see cref="Activity"/> through
        /// <see cref="Activity.AddException"/>. This is the only trace write performed by a log call,
        /// and it deliberately ignores log level filtering so that failures never stay invisible in traces.
        /// The same exception instance is recorded at most once per <see cref="Activity"/>, so reporting it again while it propagates
        /// does not duplicate the event.
        /// Default is <c>true</c>.
        /// </summary>
        public bool RecordExceptionsOnSpan { get; set; } = true;

        /// <summary>
        /// Gets or sets how a telemetry tag written more than once within a single log call is resolved.
        /// Default is <see cref="TagCollisionBehavior.KeepFirst"/>; every collision is counted and reported
        /// through the <see cref="ObservabilityTagNames.Collisions"/> tag regardless of this setting.
        /// Use <see cref="TagCollisionBehavior.Throw"/> in tests to fail on silent telemetry loss.
        /// </summary>
        public TagCollisionBehavior TagCollisions { get; set; } = TagCollisionBehavior.KeepFirst;

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
