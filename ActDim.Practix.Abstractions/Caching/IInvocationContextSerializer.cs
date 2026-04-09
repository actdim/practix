using System.Reflection;

namespace ActDim.Practix.Abstractions.Caching
{
    public interface IInvocationContextSerializer
    {
        string Serialize(MethodInfo mi, string tag, params object[] args);

        string Serialize(MethodInfo mi, InvocationContextConfig config, params object[] args);

        string Serialize(InvocationContext invocationContext);

        InvocationContext Deserialize(string value);
    }
}
