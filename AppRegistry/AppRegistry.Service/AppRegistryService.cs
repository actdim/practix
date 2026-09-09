using System;
using ActDim.AppRegistry.Repo;

namespace ActDim.AppRegistry.Service;

public class AppRegistryService : IAppRegistryService
{
    public IUserRepo Users { get; }
    public IRoleRepo Roles { get; }
    public IProjectRepo Projects { get; }

    public AppRegistryService(IUserRepo users, IRoleRepo roles, IProjectRepo projects)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));
    }
}
