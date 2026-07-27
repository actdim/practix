using ActDim.AppRegistry.Domain.Core;

namespace ActDim.AppRegistry.Repo
{
    public interface IUserRepo
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByEmailAsync(string email);
    }

    public class UserRepo : IUserRepo
    {
        static Guid Id = new Guid("10b60d35-647a-4e3e-9e92-df1ea0f4eb49");
        public async Task<User> GetByEmailAsync(string email)
        {
            return new User()
            {
                Id = Id,
                Name = "admin",
                Email = email
            };
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            if (!id.Equals(Id))
            {
                return null;
            }

            return new User()
            {
                Id = id,
                Name = "admin",
                Email = "admin@mail.com"
            };
        }
    }
}