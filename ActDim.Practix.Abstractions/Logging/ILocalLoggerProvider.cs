using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Messaging;
using System.Reflection;

namespace ActDim.Practix.Abstractions.Logging
{
    public interface ILocalLoggerProvider<in T>
    {
        IScopedLogger ForMethod(MethodBase method, IJsonSerializer jsonSerializer = null, ICallContextProvider callContextProvider = null);

        IDictionary<string, object> Enrich(object state, params (ContextProperty, object)[] properties); // KeyValuePair<ContextProperty, object>

        IDictionary<string, object> Enrich(object state, IDictionary<ContextProperty, object> properties);

        IDictionary<string, object> State(params (ContextProperty, object)[] properties); // KeyValuePair<ContextProperty, object>

        IDictionary<string, object> State(IDictionary<ContextProperty, object> properties);
    }
}
