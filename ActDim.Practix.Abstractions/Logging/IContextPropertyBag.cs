using System.Collections.Generic;

namespace ActDim.Practix.Abstractions.Logging
{
    // ILogContextPropertyBag
    public interface IContextPropertyBag
    {
        IDictionary<ContextProperty, object> Properties { get; }
    }
}
