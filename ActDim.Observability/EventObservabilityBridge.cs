using ActDim.Practix.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ActDim.Observability
{
    /// <summary>
    /// Event-centric observability bridge implementing <see cref="ILogger"/> that performs DTO object flattening and
    /// enriches <see cref="Activity"/> spans with dotted OpenTelemetry attributes.
    /// Supports ambient properties from <see cref="IAmbientContext"/> and external scopes via <see cref="IExternalScopeProvider"/>.
    /// Selective per-provider suppression is performed dynamically by decorated logger providers.
    /// </summary>
    /// <remarks>
    /// The two signals are kept strictly separate. <see cref="BeginScope{TState}"/> owns the trace side: it starts an
    /// <see cref="Activity"/> when none is current and writes the scope state, the exported ambient telemetry properties
    /// and optionally external scopes as <see cref="Activity"/> tags, independently of any log level filtering.
    /// <see cref="Log{TState}"/> owns the log side and produces a log record only; the trace context of that record is
    /// attached by the logging pipeline itself. The single exception is <see cref="Exception"/> recording, which is
    /// reported to the current span through <see cref="Activity.AddException"/> so that failures never stay invisible
    /// in traces - see <see cref="EventObservabilityOptions.RecordExceptionsOnSpan"/>.
    /// </remarks>
    public sealed class EventObservabilityBridge : ILogger, ISupportExternalScope
    {
        private readonly ILogger _inner;
        private readonly IAmbientContext? _ambientContext;
        private readonly EventObservabilityOptions _options;
        private IExternalScopeProvider? _scopeProvider;

        public EventObservabilityBridge(
            ILogger inner,
            IAmbientContext? ambientContext = null,
            EventObservabilityOptions? options = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _ambientContext = ambientContext;
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
            Activity? createdActivity = null;

            if (Activity.Current == null && _options.AutoCreateActivityOnScope)
            {
                var sourceName = ResolveActivitySourceName();
                if (!string.IsNullOrWhiteSpace(sourceName))
                {
                    var source = ActivitySourceRegistry.GetOrAdd(sourceName);
                    createdActivity = source.StartActivity(ResolveOperationName(state));
                }
            }

            EnrichSpanFromScope(state, spanWasCreated: createdActivity != null);

            var innerScope = _inner.BeginScope(state);

            if (createdActivity != null)
            {
                return new ScopeDisposable(createdActivity, innerScope);
            }

            return innerScope;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // A log call produces a log record. The span is owned by BeginScope and is deliberately left untouched here,
            // so that trace content never depends on log level filtering. Failures are the only exception to that rule.
            if (exception != null && _options.RecordExceptionsOnSpan)
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    SpanExceptionRecorder.TryRecordOnce(activity, exception);
                }
            }

            _inner.Log(logLevel, eventId, state, exception, formatter);
        }

        private string ResolveActivitySourceName()
        {
            if (_ambientContext?.Properties.TryGetValue(ObservabilityContextPropertyNames.ActivitySourceName, out var customVal) == true
                && customVal is string customName && !string.IsNullOrWhiteSpace(customName))
            {
                return customName;
            }

            return _options.DefaultActivitySourceName;
        }

        /// <summary>
        /// Derives a low-cardinality span name from the scope state. The formatted state is never used:
        /// span names identify an operation and must not carry per-call values.
        /// </summary>
        private static string ResolveOperationName<TState>(TState state)
        {
            if (state is LogEvent logEvent && !string.IsNullOrWhiteSpace(logEvent.Name))
            {
                return logEvent.Name;
            }

            if (state is IEnumerable<KeyValuePair<string, object>> values)
            {
                foreach (var value in values)
                {
                    if (string.Equals(value.Key, "{OriginalFormat}", StringComparison.Ordinal))
                    {
                        var template = value.Value?.ToString();
                        if (!string.IsNullOrWhiteSpace(template))
                        {
                            return template;
                        }
                    }
                }
            }

            if (state is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            if (state != null)
            {
                var type = state.GetType();
                if (!type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                {
                    return type.Name;
                }
            }

            return "Scope";
        }

        /// <summary>
        /// Writes explicitly exported telemetry properties, external scopes (if enabled) and the scope state onto the current span.
        /// External scopes are collected only for a span started here, since scopes opened earlier had no span to write to.
        /// </summary>
        private void EnrichSpanFromScope(object? state, bool spanWasCreated)
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            var ambientProperties = _ambientContext?.Properties;
            var includeExternalScopes = ResolveFlag(ambientProperties, ObservabilityContextPropertyNames.IncludeExternalScopes, _options.IncludeExternalScopes);

            var spanTags = new TelemetryTagCollector(_options.TagCollisions);

            if (ambientProperties != null
                && ambientProperties.TryGetValue(ObservabilityContextPropertyNames.ExportedKeys, out var rawKeys)
                && rawKeys is ImmutableHashSet<string> exportedKeys)
            {
                foreach (var key in exportedKeys)
                {
                    if (ambientProperties.TryGetValue(key, out var val))
                    {
                        spanTags.Write(EventObservabilityHelper.ToOtelName(key), val);
                    }
                }
            }

            if (spanWasCreated && includeExternalScopes)
            {
                _scopeProvider?.ForEachScope((activeScope, collector) =>
                {
                    if (activeScope != null && !EventObservabilityHelper.IsSimple(activeScope))
                    {
                        collector.WriteRange(EventObservabilityHelper.FlattenPairs(activeScope, maxDepth: _options.MaxFlattenDepth, maxAttributes: _options.MaxFlattenAttributes));
                    }
                }, spanTags);
            }

            if (state is LogEvent logEvent)
            {
                if (!string.IsNullOrWhiteSpace(logEvent.Name))
                {
                    spanTags.Write("name", logEvent.Name);
                }

                foreach (var kv in logEvent.ActivityTags)
                {
                    spanTags.Write(EventObservabilityHelper.ToOtelName(kv.Key), kv.Value);
                }
            }
            else if (state != null && !EventObservabilityHelper.IsSimple(state))
            {
                spanTags.WriteRange(EventObservabilityHelper.FlattenPairs(state, maxDepth: _options.MaxFlattenDepth, maxAttributes: _options.MaxFlattenAttributes));
            }

            ApplySpanTags(activity, spanTags);
        }

        private static bool ResolveFlag(IReadOnlyDictionary<string, object>? ambientProperties, string key, bool fallback)
        {
            if (ambientProperties != null && ambientProperties.TryGetValue(key, out var raw) && raw is bool value)
            {
                return value;
            }

            return fallback;
        }

        private static void ApplySpanTags(Activity activity, TelemetryTagCollector spanTags)
        {
            foreach (var kv in spanTags.Tags)
            {
                activity.SetTag(kv.Key, kv.Value);
            }

            if (spanTags.CollisionCount > 0)
            {
                var previous = activity.GetTagItem(ObservabilityTagNames.Collisions) as int? ?? 0;
                activity.SetTag(ObservabilityTagNames.Collisions, previous + spanTags.CollisionCount);
            }
        }

        private sealed class ScopeDisposable : IDisposable
        {
            private readonly IDisposable? _activityScope;
            private readonly IDisposable? _innerScope;

            public ScopeDisposable(IDisposable? activityScope, IDisposable? innerScope)
            {
                _activityScope = activityScope;
                _innerScope = innerScope;
            }

            public void Dispose()
            {
                _innerScope?.Dispose();
                _activityScope?.Dispose();
            }
        }
    }
}
