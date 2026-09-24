using ActDim.AppRegistry.Domain.Registry;

namespace ActDim.AppRegistry.Repo;

public interface ICollectionRepo
{
    Task<Collection> GetByIdAsync(Guid id);
}

public class CollectionRepo : ICollectionRepo
{
    public Task<Collection> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}

