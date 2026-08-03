using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.UnitTests.TestSupport;

public sealed class FakeRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = new();
    private long _nextId = 1;

    public void Seed(Role role)
    {
        role.Id = _nextId++;
        _roles.Add(role);
    }

    public Task<Role> AddAsync(Role entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        _roles.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(_roles.ToList());

    public void Update(Role entity)
    {
    }

    public void Delete(Role entity)
    {
        _roles.RemoveAll(r => r.Id == entity.Id);
    }

    public Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(_roles.Where(r => roleNames.Contains(r.Name)).ToList());

    public Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.FirstOrDefault(r => r.Name == roleName));

    public Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));

    public Task<bool> HasAssignedUsersAsync(long roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
