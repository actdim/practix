using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Disposal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace ActDim.Observability
{
    /// <summary>
    /// Default <see cref="IObservabilityContext"/> backed by <see cref="IAmbientContext"/> for ambient storage and by
    /// <see cref="Activity.Current"/> for immediate export of data properties as <see cref="Activity"/> tags.
    /// </summary>
    public sealed class ObservabilityContext : IObservabilityContext
    {
        private readonly IAmbientContext _ambientContext;

        public ObservabilityContext(IAmbientContext ambientContext)
        {
            _ambientContext = ambientContext ?? throw new ArgumentNullException(nameof(ambientContext));
        }

        private IAmbientContext AmbientContext => _ambientContext;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Properties => AmbientContext.Properties;

        /// <inheritdoc />
        public IDisposable SetStatus(string status, string? icon = null)
        {
            var statusScope = PushExported(ObservabilityContextPropertyNames.Status, status);
            if (string.IsNullOrEmpty(icon))
            {
                return statusScope;
            }

            return new CompositeDisposable(statusScope, PushExported(ObservabilityContextPropertyNames.Icon, icon!));
        }

        /// <inheritdoc />
        public IDisposable SetProgress(double percentage)
        {
            return PushExported(ObservabilityContextPropertyNames.Progress, Math.Clamp(percentage, 0.0, 100.0));
        }

        /// <inheritdoc />
        public IDisposable Push(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(name));
            }

            return PushExported(name, value);
        }

        /// <inheritdoc />
        public IDisposable PushActivitySourceName(string activitySourceName)
        {
            if (string.IsNullOrWhiteSpace(activitySourceName))
            {
                throw new ArgumentException("ActivitySource name cannot be null or whitespace.", nameof(activitySourceName));
            }

            return AmbientContext.PushProperty(ObservabilityContextPropertyNames.ActivitySourceName, activitySourceName);
        }

        /// <inheritdoc />
        public IDisposable SuppressConsole()
        {
            return AmbientContext.PushProperty(ObservabilityContextPropertyNames.SuppressConsole, true);
        }

        /// <inheritdoc />
        public IDisposable SuppressProviders(params string[] providerNames)
        {
            if (providerNames == null || providerNames.Length == 0)
            {
                return new DisposableAction(() => { });
            }

            var current = Merge(ObservabilityContextPropertyNames.SuppressedProviders, providerNames);
            return AmbientContext.PushProperty(ObservabilityContextPropertyNames.SuppressedProviders, current);
        }

        /// <inheritdoc />
        public IDisposable SuppressExternalScopes()
        {
            return AmbientContext.PushProperty(ObservabilityContextPropertyNames.IncludeExternalScopes, false);
        }

        /// <summary>
        /// Stores the property in the ambient context, marks it as an exported telemetry key, and when an Activity
        /// span is active, writes it as an Activity tag immediately. Disposing the returned handle restores previous state.
        /// </summary>
        private IDisposable PushExported(string name, object value)
        {
            var contextScope = AmbientContext.PushProperty(name, value);
            var keyScope = RegisterExportedKey(name);

            var compositeScope = new CompositeDisposable(contextScope, keyScope);

            // The Activity active at push time is captured on purpose: by the time the handle is disposed,
            // Activity.Current may already be a different one.
            var activity = Activity.Current;
            if (activity == null)
            {
                return compositeScope;
            }

            var key = EventObservabilityHelper.ToOtelName(name);
            var previous = activity.GetTagItem(key);
            activity.SetTag(key, value);

            return new SpanTagScope(compositeScope, activity, key, previous);
        }

        private IDisposable RegisterExportedKey(string key)
        {
            var current = Properties.TryGetValue(ObservabilityContextPropertyNames.ExportedKeys, out var raw)
                && raw is ImmutableHashSet<string> existing
                ? existing.Add(key)
                : ImmutableHashSet<string>.Empty.Add(key);

            return AmbientContext.PushProperty(ObservabilityContextPropertyNames.ExportedKeys, current);
        }

        private HashSet<string> Merge(string propertyName, string[] values)
        {
            var current = Properties.TryGetValue(propertyName, out var raw) && raw is HashSet<string> existing
                ? new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    current.Add(value);
                }
            }

            return current;
        }

        private sealed class SpanTagScope : IDisposable
        {
            private readonly IDisposable _contextScope;
            private readonly Activity _activity;
            private readonly string _key;
            private readonly object? _previousValue;

            public SpanTagScope(IDisposable contextScope, Activity activity, string key, object? previousValue)
            {
                _contextScope = contextScope;
                _activity = activity;
                _key = key;
                _previousValue = previousValue;
            }

            public void Dispose()
            {
                // A null value removes the attribute, which is exactly the restore for a previously absent one.
                _activity.SetTag(_key, _previousValue);
                _contextScope.Dispose();
            }
        }

        private sealed class CompositeDisposable : IDisposable
        {
            private readonly IDisposable _first;
            private readonly IDisposable _second;

            public CompositeDisposable(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                _second.Dispose();
                _first.Dispose();
            }
        }
    }
}
