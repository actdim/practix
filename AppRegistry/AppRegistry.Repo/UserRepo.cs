using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActDim.AppRegistry.Domain.Iam;
using RepoDb;

namespace ActDim.AppRegistry.Repo
{
    public interface IUserRepo
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
        Task<object> InsertAsync(User user, CancellationToken ct = default);
        Task<int> UpdateAsync(User user, CancellationToken ct = default);
        Task<int> DeleteByIdAsync(Guid id, CancellationToken ct = default);
    }

    public class UserRepo : IUserRepo
    {
        public static readonly Guid DefaultAdminId = Guid.Parse("10b60d35-647a-4e3e-9e92-df1ea0f4eb49");
        private readonly IDbConnection? _connection;
        private readonly Dictionary<Guid, User> _inMemoryUsers;

        public UserRepo(IDbConnection? connection = null)
        {
            _connection = connection;
            _inMemoryUsers = new Dictionary<Guid, User>();

            var defaultUser = new User
            {
                Id = DefaultAdminId,
                ExternalId = "10b60d35-647a-4e3e-9e92-df1ea0f4eb49",
                DisplayName = "admin",
                Email = "admin@mail.com",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _inMemoryUsers[defaultUser.Id] = defaultUser;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                var result = await _connection.QueryAsync<User>(u => u.Id == id, cancellationToken: ct);
                return result.FirstOrDefault();
            }

            return _inMemoryUsers.GetValueOrDefault(id);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                var result = await _connection.QueryAsync<User>(u => u.Email == email, cancellationToken: ct);
                return result.FirstOrDefault();
            }

            var existing = _inMemoryUsers.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing;
            }

            return new User
            {
                Id = DefaultAdminId,
                DisplayName = "admin",
                Email = email,
                ExternalId = DefaultAdminId.ToString()
            };
        }

        public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                var result = await _connection.QueryAsync<User>(u => u.ExternalId == externalId, cancellationToken: ct);
                return result.FirstOrDefault();
            }

            return _inMemoryUsers.Values.FirstOrDefault(u => string.Equals(u.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        {
            if (_connection != null)
            {
                return await _connection.QueryAllAsync<User>(cancellationToken: ct);
            }

            return _inMemoryUsers.Values.ToList();
        }

        public async Task<object> InsertAsync(User user, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }

            if (_connection != null)
            {
                return await _connection.InsertAsync<User>(user, cancellationToken: ct);
            }

            _inMemoryUsers[user.Id] = user;
            return user.Id;
        }

        public async Task<int> UpdateAsync(User user, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            if (_connection != null)
            {
                return await _connection.UpdateAsync<User>(user, cancellationToken: ct);
            }

            if (_inMemoryUsers.ContainsKey(user.Id))
            {
                _inMemoryUsers[user.Id] = user;
                return 1;
            }

            return 0;
        }

        public async Task<int> DeleteByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                return await _connection.DeleteAsync<User>(u => u.Id == id, cancellationToken: ct);
            }

            return _inMemoryUsers.Remove(id) ? 1 : 0;
        }
    }
}
