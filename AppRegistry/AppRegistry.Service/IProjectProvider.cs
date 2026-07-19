using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Service;

public interface IProjectProvider
{
    Project Project { get; }
    string ConnectionString { get; }
}
