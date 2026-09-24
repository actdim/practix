using ActDim.AppRegistry.Domain.Iam;

namespace ActDim.AppRegistry.Repo
{
    public interface IRoleRepo
    {
        Task<Role> GetByIdAsync(Guid id);
    }

    public class RoleRepo : IRoleRepo
    {
        public Task<Role> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
