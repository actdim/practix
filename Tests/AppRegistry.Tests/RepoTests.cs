using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ActDim.AppRegistry.Domain.Iam;
using ActDim.AppRegistry.Repo;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RepoDb;
using RepoDb.Options;
using Xunit;

namespace ActDim.AppRegistry.Tests
{
    public class SqliteBooleanHandler : RepoDb.Interfaces.IPropertyHandler<object?, bool>
    {
        public bool Get(object? input, PropertyHandlerGetOptions options)
        {
            if (input == null) return false;
            if (input is bool b) return b;
            return Convert.ToInt64(input) != 0;
        }

        public object? Set(bool input, PropertyHandlerSetOptions options) => input ? 1L : 0L;
    }

    public class SqliteDateTimeOffsetHandler : RepoDb.Interfaces.IPropertyHandler<object?, DateTimeOffset>
    {
        public DateTimeOffset Get(object? input, PropertyHandlerGetOptions options)
        {
            if (input == null) return default;
            if (input is DateTimeOffset dto) return dto;
            if (input is DateTime dt) return new DateTimeOffset(dt);
            if (input is string s && DateTimeOffset.TryParse(s, out var parsed)) return parsed;
            return default;
        }

        public object? Set(DateTimeOffset input, PropertyHandlerSetOptions options) => input.ToString("O");
    }

    public class SqliteNullableDateTimeOffsetHandler : RepoDb.Interfaces.IPropertyHandler<object?, DateTimeOffset?>
    {
        public DateTimeOffset? Get(object? input, PropertyHandlerGetOptions options)
        {
            if (input == null || input is DBNull) return null;
            if (input is DateTimeOffset dto) return dto;
            if (input is DateTime dt) return new DateTimeOffset(dt);
            if (input is string s && DateTimeOffset.TryParse(s, out var parsed)) return parsed;
            return null;
        }

        public object? Set(DateTimeOffset? input, PropertyHandlerSetOptions options) => input?.ToString("O");
    }

    public class SqliteJsonDocumentHandler : RepoDb.Interfaces.IPropertyHandler<object?, JsonDocument>
    {
        public JsonDocument Get(object? input, PropertyHandlerGetOptions options)
        {
            if (input == null || input is DBNull) return null!;
            if (input is JsonDocument doc) return doc;
            if (input is string s && !string.IsNullOrWhiteSpace(s))
            {
                return JsonDocument.Parse(s);
            }
            return null!;
        }

        public object? Set(JsonDocument input, PropertyHandlerSetOptions options) => input?.RootElement.GetRawText();
    }

    public class RepoTests
    {
        private static int _sqLiteInitialized;

        public RepoTests()
        {
            if (Interlocked.CompareExchange(ref _sqLiteInitialized, 1, 0) == 0)
            {
                GlobalConfiguration.Setup().UseSqlite();
                FluentMapper.Entity<User>()
                    .PropertyHandler<SqliteBooleanHandler>(u => u.IsActive)
                    .PropertyHandler<SqliteBooleanHandler>(u => u.IsEmailVerified)
                    .PropertyHandler<SqliteNullableDateTimeOffsetHandler>(u => u.LastLoginAt)
                    .PropertyHandler<SqliteDateTimeOffsetHandler>(u => u.CreatedAt)
                    .PropertyHandler<SqliteDateTimeOffsetHandler>(u => u.UpdatedAt)
                    .PropertyHandler<SqliteJsonDocumentHandler>(u => u.Settings)
                    .PropertyHandler<SqliteJsonDocumentHandler>(u => u.Metadata);
                FluentMapper.Entity<Role>()
                    .PropertyHandler<SqliteBooleanHandler>(r => r.IsSystem)
                    .PropertyHandler<SqliteDateTimeOffsetHandler>(r => r.CreatedAt)
                    .PropertyHandler<SqliteDateTimeOffsetHandler>(r => r.UpdatedAt)
                    .PropertyHandler<SqliteJsonDocumentHandler>(r => r.Metadata);
            }
        }

        [Fact]
        public void ServiceCollectionExtensions_AddAppRegistryRepo_RegistersExpectedServices()
        {
            var services = new ServiceCollection();
            services.AddAppRegistryRepo();

            Assert.Contains(services, d => d.ServiceType == typeof(CommonRepo));
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
        public async Task UserRepo_InMemory_GetByIdAsync_ReturnsUserForKnownId_AndNullForUnknownId()
        {
            var userRepo = new UserRepo();
            var knownId = UserRepo.DefaultAdminId;

            var user = await userRepo.GetByIdAsync(knownId);
            Assert.NotNull(user);
            Assert.Equal(knownId, user.Id);
            Assert.Equal("admin", user.Name);
            Assert.Equal("admin@mail.com", user.Email);

            var unknownUser = await userRepo.GetByIdAsync(Guid.NewGuid());
            Assert.Null(unknownUser);
        }

        [Fact]
        public async Task UserRepo_InMemory_GetByEmailAsync_ReturnsUserWithProvidedEmail()
        {
            var userRepo = new UserRepo();
            var user = await userRepo.GetByEmailAsync("test@domain.com");

            Assert.NotNull(user);
            Assert.Equal("test@domain.com", user.Email);
            Assert.Equal("admin", user.Name);
        }

        [Fact]
        public async Task RoleRepo_InMemory_GetByIdAsync_And_GetByNameAsync_Work()
        {
            var roleRepo = new RoleRepo();

            var admin = await roleRepo.GetByIdAsync(BuiltinRoles.Admin);
            Assert.NotNull(admin);
            Assert.Equal("admin", admin.Name);
            Assert.True(admin.IsSystem);

            var userRole = await roleRepo.GetByNameAsync("user");
            Assert.NotNull(userRole);
            Assert.Equal(BuiltinRoles.User, userRole.Id);

            var unknownRole = await roleRepo.GetByNameAsync("nonexistent");
            Assert.Null(unknownRole);
        }

        [Fact]
        public async Task CommonRepo_CheckVersionAsync_ReturnsExpectedVersion()
        {
            var commonRepo = new CommonRepo();
            var check = await commonRepo.CheckVersionAsync();

            Assert.Equal(1, check.ExpectedVersion);
            Assert.Equal(1, check.CurrentVersion);
        }

        [Fact]
        public void Constants_Schema_IsDefined()
        {
            Assert.Equal("actdim", Constants.Schema);
        }

        [Fact]
        public async Task UserRepo_RepoDb_IntegrationWithSqlite_PerformsCrud()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE users (
                        id TEXT PRIMARY KEY,
                        external_id TEXT,
                        auth_provider TEXT,
                        org_id TEXT,
                        email TEXT,
                        display_name TEXT,
                        first_name TEXT,
                        last_name TEXT,
                        avatar_url TEXT,
                        is_active BOOLEAN,
                        is_email_verified BOOLEAN,
                        last_login_at TEXT,
                        settings TEXT,
                        metadata TEXT,
                        created_at TEXT,
                        updated_at TEXT
                    );";
                await cmd.ExecuteNonQueryAsync();
            }

            var userRepo = new UserRepo(conn);
            var userId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var newUser = new User
            {
                Id = userId,
                ExternalId = "ext-repodb-1",
                Email = "repodb.user@actdim.com",
                DisplayName = "RepoDb Test User",
                FirstName = "Repo",
                LastName = "Db",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await userRepo.InsertAsync(newUser);

            var fetched = await userRepo.GetByIdAsync(userId);
            Assert.NotNull(fetched);
            Assert.Equal(userId, fetched.Id);
            Assert.Equal("repodb.user@actdim.com", fetched.Email);
            Assert.Equal("RepoDb Test User", fetched.DisplayName);

            var byEmail = await userRepo.GetByEmailAsync("repodb.user@actdim.com");
            Assert.NotNull(byEmail);
            Assert.Equal(userId, byEmail.Id);

            var byExternalId = await userRepo.GetByExternalIdAsync("ext-repodb-1");
            Assert.NotNull(byExternalId);
            Assert.Equal(userId, byExternalId.Id);

            newUser.DisplayName = "Updated RepoDb User";
            var updated = await userRepo.UpdateAsync(newUser);
            Assert.Equal(1, updated);

            var fetchedUpdated = await userRepo.GetByIdAsync(userId);
            Assert.NotNull(fetchedUpdated);
            Assert.Equal("Updated RepoDb User", fetchedUpdated.DisplayName);

            var deleted = await userRepo.DeleteByIdAsync(userId);
            Assert.Equal(1, deleted);

            var afterDelete = await userRepo.GetByIdAsync(userId);
            Assert.Null(afterDelete);
        }

        [Fact]
        public async Task RoleRepo_RepoDb_IntegrationWithSqlite_PerformsCrud()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE roles (
                        id TEXT PRIMARY KEY,
                        name TEXT,
                        description TEXT,
                        is_system BOOLEAN,
                        metadata TEXT,
                        created_at TEXT,
                        updated_at TEXT
                    );";
                await cmd.ExecuteNonQueryAsync();
            }

            var roleRepo = new RoleRepo(conn);
            var roleId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var newRole = new Role
            {
                Id = roleId,
                Name = "editor",
                Description = "Content Editor",
                IsSystem = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            await roleRepo.InsertAsync(newRole);

            var fetched = await roleRepo.GetByIdAsync(roleId);
            Assert.NotNull(fetched);
            Assert.Equal(roleId, fetched.Id);
            Assert.Equal("editor", fetched.Name);

            var byName = await roleRepo.GetByNameAsync("editor");
            Assert.NotNull(byName);
            Assert.Equal(roleId, byName.Id);

            var all = await roleRepo.GetAllAsync();
            Assert.Contains(all, r => r.Id == roleId);

            var deleted = await roleRepo.DeleteByIdAsync(roleId);
            Assert.Equal(1, deleted);

            var afterDelete = await roleRepo.GetByIdAsync(roleId);
            Assert.Null(afterDelete);
        }
    }
}
