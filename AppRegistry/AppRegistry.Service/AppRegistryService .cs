using ActDim.AppRegistry.Repo;

namespace ActDim.AppRegistry.Service;

public class AppRegistryService : IAppRegistryService
{
    public IUserRepo Users { get; }
    public IRoleRepo Roles { get; }
    public IProjectRepo Projects { get; }
}
