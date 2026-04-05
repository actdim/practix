using ActDim.Practix.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Messaging;
using Serilog.Core;
using Serilog.Events;
using System.Reflection;
using ActDim.Practix.Disposal;
using ActDim.Practix.Extensions;
using ActDim.Practix.TypeAccess.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ActDim.Practix.Logging
{
    using AbstractLogging = Abstractions.Logging; // LoggingAbstractions
    using CommonLogging = Microsoft.Extensions.Logging;

    public class LoggerProvider : AbstractLogging.ILoggerProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Lazy<ICallContextProvider> _callContextProvider;

        private readonly Lazy<IJsonSerializer> _jsonSerializer;

        public LoggerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _callContextProvider = new Lazy<ICallContextProvider>(() => _serviceProvider.GetRequiredService<ICallContextProvider>());
            _jsonSerializer = new Lazy<IJsonSerializer>(() => _serviceProvider.GetRequiredService<IJsonSerializer>());
        }

        public AbstractLogging.ILogger Get(string categoryName)
        {
            // ContextProperty.EventSource?
            var propertyName = ContextProperty.EventCategory.ToString();
            using (_callContextProvider.Value.Get().Set(propertyName, categoryName))
            {
                // Serilog sets {SourceContext} property to categoryName                
                return CreateLogger(categoryName);
            }
        }

        public IScopedLogger GetScoped(string categoryName)
        {
            return (IScopedLogger)Get(categoryName);
        }

        public IScopedLogger CreateLogger(string categoryName)
        {
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

            var logger = loggerFactory.CreateLogger(categoryName);

            return new SerilogLogger(logger, _callContextProvider.Value, _jsonSerializer.Value);
        }

        private class SerilogLogger : IScopedLogger
        {
            // private static JsonSerializerSettings DefaultFormatterSettings = new JsonSerializerSettings()
            // {
            // 	Formatting = Formatting.Indented,
            // 	ContractResolver = new DefaultContractResolver
            // 	{
            // 		NamingStrategy = new CamelCaseNamingStrategy()
            // 	},
            // 	ReferenceLoopHandling = ReferenceLoopHandling.Error,
            // 	// SerializerConstants.DateTimeFormat
            // 	DateFormatString = "yyyy-MM-dd HH:mm:ss.fff",
            // 	DateFormatHandling = DateFormatHandling.IsoDateFormat,
            // 	DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
            // 	// DateTimeZoneHandling = DateTimeZoneHandling.Utc
            // };

            private readonly CommonLogging.ILogger _loggerImpl;

            private readonly IJsonSerializer _jsonSerializer;

            private readonly ICallContextProvider _callContextProvider;

            // GenerateCorrId
            private string NewCorrId()
            {
                return new ShortId().Generate(8);
            }

            private (IDisposable, string, string) SetCorrId()
            {
                string corrId = NewCorrId();
                string parentCorrId = default; // string.Empty
                var callContext = _callContextProvider.Get();
                object value;
                if (callContext.Data.TryGetValue(CorrIdPropertyName, out value))
                {
                    parentCorrId = value?.ToString();
                }
                return (
                    new DisposableBlock<IDisposable[]>(disposers =>
                    {
                        foreach (var d in disposers)
                        {
                            d.Dispose();
                        }
                    }, [callContext.Set(CorrIdPropertyName, corrId), callContext.Set(ParentCorrIdPropertyName, parentCorrId)]),
                    corrId,
                    parentCorrId
                );

            }

            private static readonly string CorrIdPropertyName = ContextProperty.CorrelationId.ToString();
            private static readonly string ParentCorrIdPropertyName = ContextProperty.ParentCorrelationId.ToString();
            internal SerilogLogger(CommonLogging.ILogger loggerImpl, ICallContextProvider callContextProvider, IJsonSerializer jsonSerializer)
            {
                _loggerImpl = loggerImpl;
                _jsonSerializer = jsonSerializer;
                // we use global CallContextProvider here but we can create special CallContextProvider here or AsyncLocal
                _callContextProvider = callContextProvider;

                var (disposer, _, _) = SetCorrId();

                // FinalizationObserver<object>
                FinalizationObserver<SerilogLogger>.Subscribe(this, obj =>
                {
                    disposer.Dispose();
                });
            }

            public IDisposable BeginScope(AbstractLogging.LogLevel logLevel = AbstractLogging.LogLevel.Information)
            {
                return BeginScope<object>(null);
            }

            public IDisposable BeginScope<TState>(TState state, AbstractLogging.LogLevel logLevel = AbstractLogging.LogLevel.Information)
            {
                var scope = _loggerImpl.BeginScope(state);

                var (disposer, corrId, parentCorrId) = SetCorrId();

                // $"Parent scope: {parentCorrId}"
                var scopeName = $"{parentCorrId}/{corrId}";

                if (state != null)
                {
                    // new { State = state, Id = corrId, ParentId = parentCorrId }
                    scopeName = _jsonSerializer.SerializeObject(state).Replace(Environment.NewLine, string.Empty);
                }

                if (IsEnabled(logLevel))
                {
                    _loggerImpl.Log((CommonLogging.LogLevel)logLevel, default(EventId), $"Scope {scopeName} start"); // started/opened
                }

                return new DisposableAction(() =>
                {
                    disposer.Dispose();
                    if (IsEnabled(logLevel))
                    {
                        // CommonLogging.LogLevel.Information?
                        _loggerImpl.Log((CommonLogging.LogLevel)logLevel, default(EventId), $"Scope {scopeName} end"); // closed
                    }
                    scope.Dispose();
                });
            }

            public bool IsEnabled(AbstractLogging.LogLevel logLevel)
            {
                return _loggerImpl.IsEnabled((CommonLogging.LogLevel)logLevel);
            }

            public void Log<TState>(AbstractLogging.LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter == null)
                {
                    formatter = FormatToJsonMessage;
                }

                var disposers = new Lazy<IList<IDisposable>>(() => new List<IDisposable>());
                var contextProperties = GetContextProperties(state);

                var callContext = _callContextProvider.Get();

                foreach (var contextProperty in contextProperties)
                {
                    var name = contextProperty.Key.ToString();
                    disposers.Value.Add(callContext.Set(name, contextProperty.Value));
                }

                try
                {
                    _loggerImpl.Log((CommonLogging.LogLevel)logLevel, default, state, exception, formatter);
                }
                finally
                {
                    if (disposers.IsValueCreated)
                    {
                        foreach (var disposable in disposers.Value)
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }

            private IDictionary<string, object> GetContextProperties(object state)
            {
                var contextProperties = new Dictionary<string, object>();

                if (state is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        contextProperties.Add(kvp.Key, kvp.Value);
                    }
                }
                else if (state is IDictionary<ContextProperty, object> propDict)
                {
                    foreach (var kvp in propDict)
                    {
                        contextProperties.Add(kvp.Key.ToString(), kvp.Value);
                    }
                }
                else if (state is IContextPropertyBag propertyBag)
                {
                    foreach (var kvp in propertyBag.Properties)
                    {
                        contextProperties.Add(kvp.Key.ToString(), kvp.Value);
                    }
                }
                else if (state != null && !(state is System.Collections.IEnumerable))
                {
                    var type = state.GetType();
                    if (!type.IsSimple())
                    {
                        // var props = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                        //     .Where(m => (m.MemberType == MemberTypes.Field) || (m.MemberType == MemberTypes.Property && ((PropertyInfo)m).CanRead)).ToArray();
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead);
                        foreach (var p in props)
                        {
                            contextProperties.Add(p.Name, TypeAccessor.GetPropertyGetter(p).DynamicInvoke());
                        }
                    }
                }

                return contextProperties;
            }

            private string FormatToJsonMessage<TState>(TState state, Exception error)
            {
                object combinedState = null;
                if (state != null)
                {
                    if (error != null)
                    {
                        combinedState = new { State = state, Error = error };
                    }
                    else
                    {
                        combinedState = state;
                    }
                }
                else
                {
                    if (error != null)
                    {
                        combinedState = error;
                    }
                }

                return _jsonSerializer.SerializeObject(combinedState);
            }
        }
    }

    class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId, false));
        }
    }

    /// <summary>
    /// Allows custom log event property population from AsyncLocal context data (ambient data that is local to a given asynchronous control flow) 
    /// using CallContextProvider. Note: Serilog.Log.ForContext and LogContext.PushProperty doesn't work as expected!
    /// </summary>
    class CallContextEnricher : ILogEventEnricher
    {
        private readonly ICallContextProvider _callContextProvider;

        public CallContextEnricher(ICallContextProvider callContextProvider)
        {
            _callContextProvider = callContextProvider;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var callContext = _callContextProvider.Get();

            foreach (var kvp in callContext.Data)
            {
                // AddPropertyIfAbsent?
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(kvp.Key, kvp.Value));
            }
        }
    }
}