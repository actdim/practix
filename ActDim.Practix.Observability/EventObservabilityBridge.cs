#nullable enable
using ActDim.Practix.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ActDim.Practix.Observability
{
    /// <summary>
    /// Event-centric observability bridge implementing <see cref="ILogger"/> that performs DTO object flattening and 
    /// enriches current OpenTelemetry <see cref="Activity"/> spans with dotted OpenTelemetry attributes.
    /// Supports ambient properties from <see cref="ICallContextProvider"/> and external scopes via <see cref="IExternalScopeProvider"/>.
    /// Selective per-provider suppression is performed dynamically by decorated logger providers.
    /// </summary>
    public sealed class EventObservabilityBridge : ILogger, ISupportExternalScope
    {
        private readonly ILogger _inner;
        private readonly ICallContextProvider? _callContextProvider;
        private readonly EventObservabilityOptions _options;
        private IExternalScopeProvider? _scopeProvider;

        public EventObservabilityBridge(
            ILogger inner,
            ICallContextProvider? callContextProvider = null,
            EventObservabilityOptions? options = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _callContextProvider = callContextProvider;
            _options = options ?? new EventObservabilityOptions();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
            if (_inner is ISupportExternalScope innerSupport)
            {
                innerSupport.SetScopeProvider(scopeProvider);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _inner.IsEnabled(logLevel);
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state != null)
            {
                EnrichActivityFromState(state);
            }

            return _inner.BeginScope(state);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                EnrichActivityFromLogCall(eventId, state, exception, formatter);
            }

            _inner.Log(logLevel, eventId, state, exception, formatter);
        }

        private static void EnrichActivityFromState(object state)
        {
            var activity = Activity.Current;

            if (state is LogEvent logEvent)
            {
                var name = string.IsNullOrEmpty(logEvent.Name) ? logEvent.GetType().Name : logEvent.Name;
                var flat = EventObservabilityHelper.Flatten(logEvent);
                foreach (var kv in logEvent.ActivityTags)
                {
                    flat[EventObservabilityHelper.ToOtelName(kv.Key)] = kv.Value;
                }

                if (activity != null)
                {
                    foreach (var kv in flat)
                    {
                        activity.SetTag(kv.Key, kv.Value);
                    }
                }
                return;
            }

            if (EventObservabilityHelper.IsSimple(state))
            {
                return;
            }

            var flatScope = EventObservabilityHelper.Flatten(state);
            if (activity != null && flatScope.Count > 0)
            {
                foreach (var kv in flatScope)
                {
                    activity.SetTag(kv.Key, kv.Value);
                }
            }
        }

        private void EnrichActivityFromLogCall<TState>(
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string>? formatter)
        {
            var activity = Activity.Current;

            string eventName = !string.IsNullOrEmpty(eventId.Name)
                ? eventId.Name
                : (eventId.Id != 0 ? $"Event_{eventId.Id}" : "LogMessage");

            var tags = new Dictionary<string, object>();

            if (eventId.Id != 0)
            {
                tags["event.id"] = eventId.Id;
            }

            // Explicitly record formatted log message
            if (formatter != null && state != null)
            {
                try
                {
                    var formattedMessage = formatter(state, exception);
                    if (!string.IsNullOrEmpty(formattedMessage))
                    {
                        tags["message"] = formattedMessage;
                    }
                }
                catch
                {
                    // Ignore formatting errors during telemetry enrichment
                }
            }

            // Exception details
            if (exception != null)
            {
                tags["exception.type"] = exception.GetType().FullName ?? exception.GetType().Name;
                tags["exception.message"] = exception.Message;
                tags["exception.stacktrace"] = exception.ToString();
            }

            var callContextData = _callContextProvider?.Get()?.Data;

            // Resolve dynamic suppression flags from CallContext or fallback to options
            bool includeCallContext = _options.IncludeCallContext;
            if (callContextData != null && callContextData.TryGetValue(CallContextPropertyNames.IncludeCallContext, out var incCc) && incCc is bool incCcBool)
            {
                includeCallContext = incCcBool;
            }

            bool includeExternalScopes = _options.IncludeExternalScopes;
            if (callContextData != null && callContextData.TryGetValue(CallContextPropertyNames.IncludeExternalScopes, out var incExt) && incExt is bool incExtBool)
            {
                includeExternalScopes = incExtBool;
            }

            // 1. Ambient properties from CallContextProvider (if enabled)
            if (includeCallContext && callContextData != null)
            {
                foreach (var kv in callContextData)
                {
                    // Skip internal control flags from being exported as telemetry tags
                    if (kv.Key.StartsWith("__Practix_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var otelKey = EventObservabilityHelper.ToOtelName(kv.Key);
                    tags[otelKey] = kv.Value;
                }
            }

            // 2. Active external scopes from IExternalScopeProvider (if enabled)
            if (includeExternalScopes)
            {
                _scopeProvider?.ForEachScope((activeScope, _) =>
                {
                    if (activeScope != null && !EventObservabilityHelper.IsSimple(activeScope))
                    {
                        var flatScope = EventObservabilityHelper.Flatten(activeScope);
                        foreach (var kv in flatScope)
                        {
                            tags[kv.Key] = kv.Value;
                        }
                    }
                }, (object?)null);
            }

            // 3. Log call state (LogEvent / FormattedLogValues / DTO)
            if (state is LogEvent logEvent)
            {
                eventName = string.IsNullOrEmpty(logEvent.Name) ? logEvent.GetType().Name : logEvent.Name;
                var flatEvt = EventObservabilityHelper.Flatten(logEvent);
                foreach (var kv in flatEvt)
                {
                    tags[kv.Key] = kv.Value;
                }
                foreach (var kv in logEvent.ActivityTags)
                {
                    tags[EventObservabilityHelper.ToOtelName(kv.Key)] = kv.Value;
                }
            }
            else if (state is IEnumerable<KeyValuePair<string, object>> kvList)
            {
                foreach (var kv in kvList)
                {
                    if (string.Equals(kv.Key, "{OriginalFormat}", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var otelKey = EventObservabilityHelper.ToOtelName(kv.Key);
                    var value = kv.Value;

                    if (value == null || EventObservabilityHelper.IsSimple(value))
                    {
                        tags[otelKey] = value!;
                    }
                    else
                    {
                        var flatObj = EventObservabilityHelper.Flatten(value, otelKey);
                        foreach (var fkv in flatObj)
                        {
                            tags[fkv.Key] = fkv.Value;
                        }
                    }
                }
            }
            else if (state != null && !EventObservabilityHelper.IsSimple(state))
            {
                var flatState = EventObservabilityHelper.Flatten(state);
                foreach (var kv in flatState)
                {
                    tags[kv.Key] = kv.Value;
                }
            }

            if (activity != null && (tags.Count > 0 || !string.IsNullOrEmpty(eventName)))
            {
                activity.AddEvent(new ActivityEvent(eventName, tags: [.. tags.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value))]));
            }
        }
    }
}
