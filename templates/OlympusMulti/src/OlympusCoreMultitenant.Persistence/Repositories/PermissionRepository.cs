using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Permissions
            .AsNoTracking()
            .Include(permission => permission.Module)
            .ToListAsync(cancellationToken);
    }

    public async Task<Permission?> GetByNameAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        return await DbContext.Permissions
            .FirstOrDefaultAsync(permission => permission.Name == permissionName, cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsMissingModuleAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Permissions
            .Where(permission => permission.ModuleId == null)
            .ToListAsync(cancellationToken);
    }

    // Excludes permissions belonging to a since-disabled module (Core included -- it's just a
    // default-on TenantModule row now, not unconditional) or any System (platform-only) module,
    // even if a stale RolePermission row still grants it -- this is the runtime-enforcement seam
    // described in the module entitlement design, complementing the assignment-time checks in
    // RoleService. Relies on DbContext.TenantModules already being scoped to the ambient tenant via
    // its ITenantEntity query filter.
    public async Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission))
            .Where(permission => permission.Module != null && permission.Module.Kind != ModuleKind.System &&
                DbContext.TenantModules.Any(tm => tm.ModuleId == permission.ModuleId))
            .Select(permission => permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
