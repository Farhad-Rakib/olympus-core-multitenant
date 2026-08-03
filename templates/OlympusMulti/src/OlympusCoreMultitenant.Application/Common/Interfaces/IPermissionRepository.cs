using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<Permission?> GetByNameAsync(string permissionName, CancellationToken cancellationToken = default);

    // Tracked (not AsNoTracking) on purpose: callers mutate these in place and rely on EF's
    // identity resolution against any already-tracked Permission instance in the same DbContext,
    // instead of attaching a detached copy that could collide with one (see PermissionSyncService).
    Task<IReadOnlyList<Permission>> GetPermissionsMissingModuleAsync(CancellationToken cancellationToken = default);
}
