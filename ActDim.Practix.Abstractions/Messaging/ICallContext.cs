namespace ActDim.Practix.Abstractions.Messaging // ActDim.Practix.Abstractions.CallContext
{
    public interface ICallContext
    {
        // IDisposable Set(CallContextProperty property, object value); // Push
        IDisposable Set(string name, object value); // Push
        IReadOnlyDictionary<string, object> Data { get; }
    }
}