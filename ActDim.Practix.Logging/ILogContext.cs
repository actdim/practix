using ActDim.Practix.Abstractions.Logging;

namespace ActDim.Practix.Logging
{
    // TODO: implement
    public interface ILogContext
    {
        IDisposable Set(ContextProperty property, object value); // Push
        IDisposable Set(string name, object value); // Push
        IReadOnlyDictionary<string, object> Data
        {
            get;
        }
    }
}
