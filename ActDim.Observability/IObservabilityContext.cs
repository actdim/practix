using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ActDim.Observability
{
    /// <summary>
    /// Ambient observability context of the current asynchronous flow: the properties that describe the operation
    /// being executed, and the per-scope switches of the telemetry pipeline.
    /// </summary>
    /// <remarks>
    /// This is deliberately a separate concept from <see cref="ActDim.Practix.Abstractions.Context.IAmbientContext"/>, which is a neutral
    /// ambient variable store with no telemetry meaning. Values set here acquire telemetry meaning immediately:
    /// data properties are written to the current <see cref="Activity"/> as tags (OpenTelemetry span attributes) as they
    /// are set, and restored when the returned handle is disposed. Properties set before an <see cref="Activity"/> exists
    /// are still picked up when <see cref="EventObservabilityBridge.BeginScope{TState}"/> starts one.
    /// Control switches are never exported - they only configure the pipeline for the duration of the scope.
    /// </remarks>
    public interface IObservabilityContext
    {
        /// <summary>
        /// Gets the ambient observability properties of the current asynchronous flow, control keys included.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the current ambient execution status, or <see langword="null"/> if no status is currently active.
        /// </summary>
        ObservabilityStatus? Status { get; }

        /// <summary>
        /// Sets the current execution status state (name, progress percentage, icon, step index) for the duration of the returned handle,
        /// exporting it as an <see cref="Activity"/> tag.
        /// </summary>
        IDisposable SetStatus(ObservabilityStatus status);

        /// <summary>
        /// Sets the current execution status state for the duration of the returned handle,
        /// exporting it as an <see cref="Activity"/> tag.
        /// </summary>
        IDisposable SetStatus(string? name = null, double? progress = null, string? icon = null, int? step = null, int? totalSteps = null);

        /// <summary>
        /// Sets an arbitrary telemetry property for the duration of the returned handle,
        /// exporting it as an <see cref="Activity"/> tag.
        /// </summary>
        IDisposable Push(string name, object value);

        /// <summary>
        /// Sets the <see cref="ActivitySource"/> name used when an <see cref="Activity"/> is started automatically on BeginScope.
        /// Not exported as an <see cref="Activity"/> tag.
        /// </summary>
        IDisposable PushActivitySourceName(string activitySourceName);

        /// <summary>
        /// Suppresses log output to console logger providers for the returned scope, while retaining <see cref="Activity"/>
        /// enrichment and every other logger provider.
        /// </summary>
        IDisposable SuppressConsole();

        /// <summary>
        /// Suppresses log output to specific logger providers by alias or name (e.g. "Console", "File", "Otlp")
        /// for the returned scope.
        /// </summary>
        IDisposable SuppressProviders(params string[] providerNames);

        /// <summary>
        /// Stops external scopes (e.g. ASP.NET Core, HttpClient) from being written into <see cref="Activity"/> tags
        /// for the returned scope.
        /// </summary>
        IDisposable SuppressExternalScopes();
    }
}
