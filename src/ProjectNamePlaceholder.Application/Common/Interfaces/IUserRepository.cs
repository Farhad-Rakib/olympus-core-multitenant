using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAcrossTenantsAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByIdsAcrossTenantsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(CancellationToken cancellationToken = default);
}
