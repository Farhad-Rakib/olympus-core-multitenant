using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Roles
            .AsNoTracking()
            .Where(role => role.TenantId == DbContext.CurrentTenantId)
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var normalized = roleNames.Select(r => r.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await DbContext.Roles
            .Where(r => normalized.Contains(r.Name))
            .ToListAsync(cancellationToken);
    }

    // Explicit TenantId filter: see UserRepository.GetByEmailAsync for why the automatic global
    // query filter is not sufficient for Include()+FirstOrDefaultAsync on this shape.
    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await DbContext.Roles
            .Where(role => role.TenantId == DbContext.CurrentTenantId)
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Roles
            .Where(role => role.TenantId == DbContext.CurrentTenantId)
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
    }

    public async Task<bool> HasAssignedUsersAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(userRole => userRole.RoleId == roleId, cancellationToken);
    }
}
