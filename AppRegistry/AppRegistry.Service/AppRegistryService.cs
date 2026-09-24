using System;
using ActDim.AppRegistry.Repo;

namespace ActDim.AppRegistry.Service;

public class AppRegistryService : IAppRegistryService
{
    public IUserRepo Users { get; }
    public IRoleRepo Roles { get; }
    public ICollectionRepo Collections { get; }

    public AppRegistryService(IUserRepo users, IRoleRepo roles, ICollectionRepo collections)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        Collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }
}
