#nullable enable
using ActDim.Practix.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ActDim.Observability
{
    /// <summary>
    /// Decorator over <see cref="ILoggerFactory"/> that decorates registered <see cref="ILoggerProvider"/> instances 
    /// to support per-provider selective suppression based on ProviderAliasAttribute and <see cref="ObservabilityContextPropertyNames"/>.
    /// </summary>
    public sealed class EventObservabilityLoggerFactory : ILoggerFactory, ISupportExternalScope
    {
        private readonly ILoggerFactory _inner;
        private readonly IAmbientContextProvider? _ambientContextProvider;
        private readonly EventObservabilityOptions _options;
        private IExternalScopeProvider? _scopeProvider;

        public EventObservabilityLoggerFactory(
            ILoggerFactory inner,
            IAmbientContextProvider? ambientContextProvider = null,
            IExternalScopeProvider? scopeProvider = null,
            EventObservabilityOptions? options = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _ambientContextProvider = ambientContextProvider;
            _scopeProvider = scopeProvider;
            _options = options ?? new EventObservabilityOptions();

            if (_inner is ISupportExternalScope innerSupport && _scopeProvider != null)
            {
                innerSupport.SetScopeProvider(_scopeProvider);
            }
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
            if (_inner is ISupportExternalScope innerSupport)
            {
                innerSupport.SetScopeProvider(scopeProvider);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _inner.CreateLogger(categoryName);
            var logger = new EventObservabilityBridge(innerLogger, _ambientContextProvider, _options);
            if (_scopeProvider != null)
            {
                logger.SetScopeProvider(_scopeProvider);
            }

            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var alias = ResolveProviderAlias(provider, _options);
            var decoratedProvider = new EventObservabilityProviderDecorator(provider, alias, _ambientContextProvider, _scopeProvider);

            if (_scopeProvider != null && decoratedProvider is ISupportExternalScope support)
            {
                support.SetScopeProvider(_scopeProvider);
            }

            _inner.AddProvider(decoratedProvider);
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        internal static string ResolveProviderAlias(ILoggerProvider provider, EventObservabilityOptions options)
        {
            var type = provider.GetType();
            if (options.CustomProviderAliases.TryGetValue(type, out var customAlias))
            {
                return customAlias;
            }

            var aliasAttr = type.GetCustomAttributes(inherit: true)
                .FirstOrDefault(a => a.GetType().Name == "ProviderAliasAttribute" || a.GetType().Name == "TestProviderAliasAttribute");

            if (aliasAttr != null)
            {
                var aliasVal = aliasAttr.GetType().GetProperty("Alias")?.GetValue(aliasAttr)?.ToString();
                if (!string.IsNullOrWhiteSpace(aliasVal))
                {
                    return aliasVal;
                }
            }

            var name = type.Name;
            if (name.EndsWith("LoggerProvider", StringComparison.Ordinal))
            {
                name = name[..^"LoggerProvider".Length];
            }
            else if (name.EndsWith("Provider", StringComparison.Ordinal))
            {
                name = name[..^"Provider".Length];
            }

            return name;
        }
    }

    internal sealed class EventObservabilityProviderDecorator : ILoggerProvider, ISupportExternalScope
    {
        private readonly ILoggerProvider _inner;
        private readonly string _alias;
        private readonly IAmbientContextProvider? _ambientContextProvider;
        private IExternalScopeProvider? _scopeProvider;

        public EventObservabilityProviderDecorator(
            ILoggerProvider inner,
            string alias,
            IAmbientContextProvider? ambientContextProvider,
            IExternalScopeProvider? scopeProvider)
        {
            _inner = inner;
            _alias = alias;
            _ambientContextProvider = ambientContextProvider;
            _scopeProvider = scopeProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _inner.CreateLogger(categoryName);
            return new EventObservabilityProviderLogger(innerLogger, _alias, _ambientContextProvider);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
            if (_inner is ISupportExternalScope support)
            {
                support.SetScopeProvider(scopeProvider);
            }
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }

    internal sealed class EventObservabilityProviderLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly string _alias;
        private readonly IAmbientContextProvider? _ambientContextProvider;

        public EventObservabilityProviderLogger(ILogger inner, string alias, IAmbientContextProvider? ambientContextProvider)
        {
            _inner = inner;
            _alias = alias;
            _ambientContextProvider = ambientContextProvider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _inner.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _inner.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var ambientProperties = _ambientContextProvider?.Get()?.Properties;
            if (ambientProperties != null)
            {
                // Check SuppressConsole flag
                bool suppressConsole = ambientProperties.TryGetValue(ObservabilityContextPropertyNames.SuppressConsole, out var suppCon) && suppCon is bool suppConBool && suppConBool;
                if (suppressConsole && string.Equals(_alias, "Console", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Check SuppressedProviders list
                if (ambientProperties.TryGetValue(ObservabilityContextPropertyNames.SuppressedProviders, out var suppProvs) && suppProvs is HashSet<string> suppressedSet)
                {
                    if (suppressedSet.Contains(_alias) || suppressedSet.Contains(_inner.GetType().Name))
                    {
                        return;
                    }
                }
            }

            _inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
