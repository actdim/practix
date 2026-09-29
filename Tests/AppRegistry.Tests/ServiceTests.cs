using System;
using System.Threading.Tasks;
using ActDim.AppRegistry.Repo;
using ActDim.AppRegistry.Service;
using ActDim.Practix.Service;
using ActDim.Practix.Service.Settings;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActDim.AppRegistry.Tests
{
    public class ServiceTests
    {
        [Fact]
        public void AppRegistryService_Constructor_ThrowsOnNullArguments()
        {
            var userRepo = new UserRepo();
            var roleRepo = new RoleRepo();

            Assert.Throws<ArgumentNullException>(() => new AppRegistryService(null!, roleRepo));
            Assert.Throws<ArgumentNullException>(() => new AppRegistryService(userRepo, null!));
        }

        [Fact]
        public void AppRegistryService_Properties_AreAssignedCorrectly()
        {
            var userRepo = new UserRepo();
            var roleRepo = new RoleRepo();

            var service = new AppRegistryService(userRepo, roleRepo);

            Assert.Same(userRepo, service.Users);
            Assert.Same(roleRepo, service.Roles);
        }

        [Fact]
        public void ServiceCollectionExtensions_AddAppRegistryService_RegistersExpectedServices()
        {
            var services = new ServiceCollection();
            services.AddAppRegistryService();

            Assert.Contains(services, d => d.ServiceType == typeof(IAppRegistryService) && d.ImplementationType == typeof(AppRegistryService));
        }

        [Fact]
        public void ServiceCollectionExtensions_AddAppRegistryService_ThrowsOnNullServices()
        {
            IServiceCollection nullServices = null!;
            Assert.Throws<ArgumentNullException>(() => nullServices.AddAppRegistryService());
        }

        [Fact]
        public async Task AppContext_GetAccessTokenAsync_And_ValidateAccessTokenAsync_Roundtrip()
        {
            var knownId = "10b60d35-647a-4e3e-9e92-df1ea0f4eb49";
            var userRepo = new UserRepo();
            var roleRepo = new RoleRepo();
            var appRegService = new AppRegistryService(userRepo, roleRepo);

            var appContext = new ActDim.AppRegistry.Service.AppContext(appRegService);

            var authConfig = new AuthConfig
            {
                LocalJwt = new LocalAuthJwtConfig
                {
                    Issuer = "test-issuer",
                    DefaultAudience = "test-audience",
                    IssuerSigningKey = "secret-key-at-least-32-bytes-long-for-hmac-sha256!",
                    Validation = new TokenValidationConfig
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateSigningKey = true,
                        ClockSkewSeconds = 5
                    }
                }
            };

            var userInfo = new UserInfo(knownId, "admin");

            // 1. Generate token
            var token = await appContext.GetAccessTokenAsync(userInfo, authConfig);
            Assert.False(string.IsNullOrWhiteSpace(token));

            // 2. Validate token
            await appContext.ValidateAccessTokenAsync(token, authConfig);
        }

        [Fact]
        public async Task AppContext_GetAccessTokenAsync_ThrowsOnUnsupportedAuth()
        {
            var appRegService = new AppRegistryService(new UserRepo(), new RoleRepo());
            var appContext = new ActDim.AppRegistry.Service.AppContext(appRegService);

            var authConfig = new AuthConfig { LocalJwt = null };
            var userInfo = new UserInfo(Guid.NewGuid().ToString(), "user");

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await appContext.GetAccessTokenAsync(userInfo, authConfig);
            });
        }
    }
}
