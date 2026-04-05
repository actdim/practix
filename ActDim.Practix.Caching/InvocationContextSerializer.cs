using System.Reflection;
using ActDim.Practix.Abstractions.Caching;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Caching.Extensions;

namespace ActDim.Practix.Caching
{
    public class InvocationContextSerializer : IInvocationContextSerializer
    {
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Missing
        /// </summary>
        private static readonly object Missing = new { }; // new object()

        public InvocationContextSerializer(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public InvocationContext Deserialize(string value)
        {
            return _jsonSerializer.DeserializeObject<InvocationContext>(value);
        }

        public string Serialize(MethodInfo mi, string tag, params object[] args)
        {
            return Serialize(new InvocationContext()
            {
                MethodId = mi.GetMetadataKey(),
                GenericArgumentIds = mi.GetGenericArguments().Select(t => t.GetMetadataKey()).ToArray(),
                Tag = tag,
                Arguments = args
            });
        }

        public string Serialize(MethodInfo mi, InvocationContextConfig config, params object[] args)
        {
            if (config != null)
            {
                if (config.ExcludeParameterIndexes != null)
                {
                    args = args.Select((a, i) => config.ExcludeParameterIndexes.Contains(i) ? Missing : a).ToArray();
                }
                if (config.ExcludeParameterTypes != null)
                {
                    var argTypes = mi.GetParameters().Select(pi => pi.ParameterType).ToArray();
                    args = args.Select((a, i) => config.ExcludeParameterTypes.Contains(argTypes[i]) ? Missing : a).ToArray();
                }
            }

            return Serialize(mi, config == null ? null : config.Tag, args);
        }

        public string Serialize(InvocationContext invocationContext)
        {
            return _jsonSerializer.SerializeObject(invocationContext);
        }
    }
}
