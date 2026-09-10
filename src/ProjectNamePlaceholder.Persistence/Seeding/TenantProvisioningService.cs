using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.ModuleSystem;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Seeding;

public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IRbacSeeder _rbacSeeder;
    private readonly IReadOnlyList<IOlympusModule> _modules;

    public TenantProvisioningService(
        ApplicationDbContext dbContext,
        ICurrentTenantService currentTenantService,
        IRbacSeeder rbacSeeder,
        IReadOnlyList<IOlympusModule> modules)
    {
        _dbContext = dbContext;
        _currentTenantService = currentTenantService;
        _rbacSeeder = rbacSeeder;
        _modules = modules;
    }

    // Deliberately does NOT seed any user here (previously did, via the now-removed
    // DefaultUserSeeder call -- see PlatformAdminSeeder for why that was a security problem).
    // Activate leaves user creation to the admin via the Users page; Approve creates the real
    // admin from what the invitee submitted. Only the one-time global bootstrap admin (seeded
    // separately by EfCoreDatabaseBootstrapper, not per-tenant) is exempt from that rule.
    public async Task ProvisionAsync(Tenant tenant, IReadOnlyList<string>? moduleKeys = null, CancellationToken cancellationToken = default)
    {
        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        // Module entitlements must exist before RbacSeeder runs -- it now only grants Admin
        // permissions from modules this tenant is actually entitled to.
        await SeedModuleEntitlementsAsync(moduleKeys, cancellationToken);
        await _rbacSeeder.SeedAsync(cancellationToken);
        await DefaultSiteSettingsSeeder.SeedAsync(_dbContext, tenant.Name);
        await DefaultMenuSeeder.SeedAsync(_dbContext, _modules);
    }

    public async Task ResyncCatalogAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        await _rbacSeeder.SeedAsync(cancellationToken);
        await DefaultSiteSettingsSeeder.SeedAsync(_dbContext, tenant.Name);
        await DefaultMenuSeeder.SeedAsync(_dbContext, _modules);
    }

    // Core is no longer unconditional -- it's just the module that's entitled by default when no
    // explicit list is given, preserving today's "just provision it, it works" happy path. A caller
    // that supplies an explicit list gets exactly that list (which could deliberately omit Core).
    // System-kind modules are filtered out defensively even if requested by key.
    private async Task SeedModuleEntitlementsAsync(IReadOnlyList<string>? moduleKeys, CancellationToken cancellationToken)
    {
        var requestedKeys = (moduleKeys is null || moduleKeys.Count == 0)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Modules.Core }
            : moduleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var modules = await _dbContext.Modules
            .Where(m => m.Kind != ModuleKind.System && requestedKeys.Contains(m.Key))
            .ToListAsync(cancellationToken);

        // Idempotent: ProvisionAsync can run again against an already-provisioned tenant (the
        // bootstrap "default" tenant is (re)provisioned on every app start), so only insert rows
        // that don't already exist.
        var alreadyEntitledIds = (await _dbContext.TenantModules.Select(tm => tm.ModuleId).ToListAsync(cancellationToken)).ToHashSet();
        var toAdd = modules.Where(m => !alreadyEntitledIds.Contains(m.Id)).ToList();

        foreach (var module in toAdd)
        {
            _dbContext.TenantModules.Add(new TenantModule { ModuleId = module.Id });
        }

        if (toAdd.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
