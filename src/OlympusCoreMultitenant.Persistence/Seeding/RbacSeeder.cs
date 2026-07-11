using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Seeding;

public sealed class RbacSeeder : IRbacSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RbacSeeder> _logger;

    public RbacSeeder(ApplicationDbContext dbContext, ILogger<RbacSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var roles = new[]
        {
            new Role(SystemRoles.SuperAdmin, "Full system access."),
            new Role(SystemRoles.Admin, "Administrative management access."),
            new Role(SystemRoles.User, "Basic application user access.")
        };

        foreach (var role in roles)
        {
            var existingRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == role.Name, cancellationToken);
            if (existingRole is null)
            {
                await _dbContext.Roles.AddAsync(role, cancellationToken);
            }
        }

        foreach (var permissionName in Permissions.All)
        {
            var existingPermission = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName, cancellationToken);
            if (existingPermission is null)
            {
                await _dbContext.Permissions.AddAsync(new Permission(permissionName, $"Permission for {permissionName}."), cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roleNames = roles.Select(x => x.Name).ToHashSet();
        var permissionNames = Permissions.All.ToHashSet();

        var persistedRoles = await _dbContext.Roles.Where(r => roleNames.Contains(r.Name)).ToListAsync(cancellationToken);
        var persistedPermissions = await _dbContext.Permissions.Include(p => p.Module)
            .Where(p => permissionNames.Contains(p.Name)).ToListAsync(cancellationToken);


        var admin = persistedRoles.Single(r => r.Name == SystemRoles.Admin);
        var user = persistedRoles.Single(r => r.Name == SystemRoles.User);

        var rolePermissions = new List<RolePermission>();
        var existingPairs = await _dbContext.RolePermissions
            .Select(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
            .ToListAsync(cancellationToken);
        var existingSet = existingPairs.Select(pair => (pair.RoleId, pair.PermissionId)).ToHashSet();

        // Runs inside the caller's already-open BeginScope (TenantProvisioningService.ProvisionAsync),
        // so this is scoped to the tenant being seeded. By the time this runs, module entitlements
        // (Core included -- it's a default-on TenantModule row now, not unconditional) have already
        // been seeded by SeedModuleEntitlementsAsync, which runs first.
        var entitledModuleIds = (await _dbContext.TenantModules.Select(tm => tm.ModuleId).ToListAsync(cancellationToken)).ToHashSet();

        // NOTE: SuperAdmin bypasses permission checks at runtime; do NOT assign explicit permissions here.

        // Admin gets every permission from a module this tenant is currently entitled to, minus a
        // small set of destructive operations it shouldn't have by default. Platform/System module
        // permissions (TenantsManage, SystemSettingsManage, Permissions.* CRUD, etc.) are structurally
        // excluded here since Modules.Platform is never entitled to any tenant -- see
        // Application/Security/Modules.cs.
        foreach (var permission in persistedPermissions.Where(p =>
            p.Module is not null && p.Module.Kind != ModuleKind.System &&
            p.ModuleId.HasValue && entitledModuleIds.Contains(p.ModuleId.Value) &&
            p.Name is not Permissions.UsersDelete and not Permissions.RolesDelete and not Permissions.UserRolesDelete and not Permissions.RolePermissionsDelete))
        {
            if (existingSet.Add((admin.Id, permission.Id)))
            {
                rolePermissions.Add(new RolePermission { RoleId = admin.Id, PermissionId = permission.Id });
            }
        }

        var usersReadPermission = persistedPermissions.Single(p => p.Name == Permissions.UsersRead);
        if (usersReadPermission.ModuleId.HasValue && entitledModuleIds.Contains(usersReadPermission.ModuleId.Value) &&
            existingSet.Add((user.Id, usersReadPermission.Id)))
        {
            rolePermissions.Add(new RolePermission
            {
                RoleId = user.Id,
                PermissionId = usersReadPermission.Id
            });
        }

        if (rolePermissions.Count > 0)
        {
            await _dbContext.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("RBAC seed completed successfully.");
    }
}
