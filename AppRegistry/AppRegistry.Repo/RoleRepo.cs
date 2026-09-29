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
    public interface IRoleRepo
    {
        Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<IEnumerable<Role>> GetAllAsync(CancellationToken ct = default);
        Task<object> InsertAsync(Role role, CancellationToken ct = default);
        Task<int> UpdateAsync(Role role, CancellationToken ct = default);
        Task<int> DeleteByIdAsync(Guid id, CancellationToken ct = default);
    }

    public class RoleRepo : IRoleRepo
    {
        private readonly IDbConnection? _connection;
        private readonly Dictionary<Guid, Role> _inMemoryRoles;

        public RoleRepo(IDbConnection? connection = null)
        {
            _connection = connection;
            _inMemoryRoles = new Dictionary<Guid, Role>();

            var adminRole = new Role
            {
                Id = BuiltinRoles.Admin,
                Name = "admin",
                Description = "Administrator",
                IsSystem = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var userRole = new Role
            {
                Id = BuiltinRoles.User,
                Name = "user",
                Description = "User",
                IsSystem = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _inMemoryRoles[adminRole.Id] = adminRole;
            _inMemoryRoles[userRole.Id] = userRole;
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                var result = await _connection.QueryAsync<Role>(r => r.Id == id, cancellationToken: ct);
                return result.FirstOrDefault();
            }

            return _inMemoryRoles.GetValueOrDefault(id);
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                var result = await _connection.QueryAsync<Role>(r => r.Name == name, cancellationToken: ct);
                return result.FirstOrDefault();
            }

            return _inMemoryRoles.Values.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken ct = default)
        {
            if (_connection != null)
            {
                return await _connection.QueryAllAsync<Role>(cancellationToken: ct);
            }

            return _inMemoryRoles.Values.ToList();
        }

        public async Task<object> InsertAsync(Role role, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(role, nameof(role));

            if (role.Id == Guid.Empty)
            {
                role.Id = Guid.NewGuid();
            }

            if (_connection != null)
            {
                return await _connection.InsertAsync<Role>(role, cancellationToken: ct);
            }

            _inMemoryRoles[role.Id] = role;
            return role.Id;
        }

        public async Task<int> UpdateAsync(Role role, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(role, nameof(role));

            if (_connection != null)
            {
                return await _connection.UpdateAsync<Role>(role, cancellationToken: ct);
            }

            if (_inMemoryRoles.ContainsKey(role.Id))
            {
                _inMemoryRoles[role.Id] = role;
                return 1;
            }

            return 0;
        }

        public async Task<int> DeleteByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_connection != null)
            {
                return await _connection.DeleteAsync<Role>(r => r.Id == id, cancellationToken: ct);
            }

            return _inMemoryRoles.Remove(id) ? 1 : 0;
        }
    }
}
