using ActDim.AppRegistry.Repo;

namespace ActDim.AppRegistry.Service;

public interface IAppRegistryService
{
    IUserRepo Users { get; }
    IRoleRepo Roles { get; }
    IProjectRepo Projects { get; }
}
