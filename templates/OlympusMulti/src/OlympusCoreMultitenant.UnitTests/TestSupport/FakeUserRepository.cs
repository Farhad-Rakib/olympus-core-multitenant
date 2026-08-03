using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.UnitTests.TestSupport;

public sealed class FakeUserRepository : IUserRepository
{
    public readonly List<User> Users = new();
    private long _nextId = 1;

    public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        Users.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(Users.ToList());

    public void Update(User entity)
    {
    }

    public void Delete(User entity)
    {
        Users.RemoveAll(u => u.Id == entity.Id);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByEmailAcrossTenantsAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task<IReadOnlyList<User>> GetByIdsAcrossTenantsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(Users.Where(u => ids.Contains(u.Id)).ToList());

    public Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<int> CountByTenantAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Count);
}
