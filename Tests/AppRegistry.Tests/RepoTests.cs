using System;
using System.Threading.Tasks;
using ActDim.AppRegistry.Repo;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActDim.AppRegistry.Tests
{
    public class RepoTests
    {
        [Fact]
        public void ServiceCollectionExtensions_AddAppRegistryRepo_RegistersExpectedServices()
        {
            var services = new ServiceCollection();
            services.AddAppRegistryRepo();

            Assert.Contains(services, d => d.ServiceType == typeof(CommonRepo));
            Assert.Contains(services, d => d.ServiceType == typeof(IProjectRepo) && d.ImplementationType == typeof(ProjectRepo));
            Assert.Contains(services, d => d.ServiceType == typeof(IRoleRepo) && d.ImplementationType == typeof(RoleRepo));
            Assert.Contains(services, d => d.ServiceType == typeof(IUserRepo) && d.ImplementationType == typeof(UserRepo));
        }

        [Fact]
        public void ServiceCollectionExtensions_AddAppRegistryRepo_ThrowsOnNullServices()
        {
            IServiceCollection nullServices = null!;
            Assert.Throws<ArgumentNullException>(() => nullServices.AddAppRegistryRepo());
        }

        [Fact]
        public async Task UserRepo_GetByIdAsync_ReturnsUserForKnownId_AndNullForUnknownId()
        {
            var userRepo = new UserRepo();
            var knownId = Guid.Parse("10b60d35-647a-4e3e-9e92-df1ea0f4eb49");

            var user = await userRepo.GetByIdAsync(knownId);
            Assert.NotNull(user);
            Assert.Equal(knownId, user.Id);
            Assert.Equal("admin", user.Name);
            Assert.Equal("admin@mail.com", user.Email);

            var unknownUser = await userRepo.GetByIdAsync(Guid.NewGuid());
            Assert.Null(unknownUser);
        }

        [Fact]
        public async Task UserRepo_GetByEmailAsync_ReturnsUserWithProvidedEmail()
        {
            var userRepo = new UserRepo();
            var user = await userRepo.GetByEmailAsync("test@domain.com");

            Assert.NotNull(user);
            Assert.Equal("test@domain.com", user.Email);
            Assert.Equal("admin", user.Name);
        }

        [Fact]
        public void Constants_Schema_IsDefined()
        {
            Assert.Equal("actdim", Constants.Schema);
        }
    }
}
