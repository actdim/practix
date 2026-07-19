using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Repo
{
    public interface IProjectRepo
    {
        Task<Project> GetByIdAsync(Guid id);
    }

    public class ProjectRepo : IProjectRepo
    {
        public Task<Project> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}