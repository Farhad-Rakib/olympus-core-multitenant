using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAcrossTenantsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(CancellationToken cancellationToken = default);
}
