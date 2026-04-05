using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Logging;
using ActDim.Practix.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ActDim.Practix.Logging
{
    internal class LocalLoggerProviderGeneric<T> : ILocalLoggerProvider<T>
    {
        private readonly ILoggerProvider _loggerProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ICallContextProvider _callContextProvider;

        public LocalLoggerProviderGeneric(ILoggerProvider loggerProvider, IJsonSerializer jsonSerializer, ICallContextProvider callContextProvider)
        {
            _loggerProvider = loggerProvider;
            _jsonSerializer = jsonSerializer;
            _callContextProvider = callContextProvider;
        }

        public IScopedLogger ForMethod(MethodBase method, IJsonSerializer jsonSerializer = null, ICallContextProvider callContextProvider = null)
        {
            return _loggerProvider.ToLocal<T>()(method, jsonSerializer ?? _jsonSerializer, callContextProvider ?? _callContextProvider);
        }

        // TODO: move Enrich and State methods to base class for LoggerProvider and LocalLoggerProvider

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="properties"></param>
        /// <returns>Dictionary<string, object></returns>
        public IDictionary<string, object> Enrich(object state, params (ContextProperty, object)[] properties) // KeyValuePair<ContextProperty, object>
        {
            var sources = new List<object>();

            if (state != default)
            {
                if (state is System.Collections.IEnumerable)
                {
                    // https://stackoverflow.com/questions/123181/testing-if-an-object-is-a-dictionary-in-c-sharp                    
                    // TODO: improve this dictionary check 
                    if (state is System.Collections.IDictionary)
                    {
                        throw new NotSupportedException("Cannot enrich this type of collection object");
                    }
                }
                sources.Add(state);
            }
            if (properties.Length > 0)
            {
                var propertyDictionary = new Dictionary<ContextProperty, object>();
                foreach (var property in properties)
                {
                    propertyDictionary[property.Item1] = property.Item2;
                }
                sources.Add(propertyDictionary);
            }
            if (sources.Count > 0)
            {
                return _jsonSerializer
                    // <JObject>?
                    .DeserializeObject<Dictionary<string, object>>(_jsonSerializer.MergeAndSerializeObject(sources));
            }
            return default;
        }

        public IDictionary<string, object> Enrich(object state, IDictionary<ContextProperty, object> properties)
        {
            var sources = new List<object>();
            if (state != default)
            {
                if (state is System.Collections.IEnumerable)
                {
                    // https://stackoverflow.com/questions/123181/testing-if-an-object-is-a-dictionary-in-c-sharp                    
                    // TODO: improve this dictionary check 
                    if (state is System.Collections.IDictionary)
                    {
                        throw new NotSupportedException("Cannot enrich this type of collection object");
                    }
                }
                sources.Add(state);
            }
            if (properties.Count > 0)
            {
                sources.Add(properties);
            }
            if (sources.Count > 0)
            {
                return _jsonSerializer
                // <JObject>?
                .DeserializeObject<Dictionary<string, object>>(_jsonSerializer.MergeAndSerializeObject(sources));
            }
            return default;
        }

        public IDictionary<string, object> State(params (ContextProperty, object)[] properties) // KeyValuePair<ContextProperty, object>
        {
            return Enrich(null, properties);
        }

        public IDictionary<string, object> State(IDictionary<ContextProperty, object> properties)
        {
            return Enrich(null, properties);
        }
    }
}