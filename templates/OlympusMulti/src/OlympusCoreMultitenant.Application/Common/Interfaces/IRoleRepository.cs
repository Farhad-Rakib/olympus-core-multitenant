using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> HasAssignedUsersAsync(long roleId, CancellationToken cancellationToken = default);
}
