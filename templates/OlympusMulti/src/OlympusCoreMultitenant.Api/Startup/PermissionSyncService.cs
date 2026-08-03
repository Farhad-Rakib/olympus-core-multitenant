using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.ModuleSystem;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Api.Startup;

public sealed class PermissionSyncService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PermissionSyncService> _logger;
    private readonly IReadOnlyList<IOlympusModule> _modules;

    public PermissionSyncService(
        IPermissionRepository permissionRepository,
        IModuleRepository moduleRepository,
        IUnitOfWork unitOfWork,
        ILogger<PermissionSyncService> logger,
        IReadOnlyList<IOlympusModule> modules)
    {
        _permissionRepository = permissionRepository;
        _moduleRepository = moduleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _modules = modules;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _permissionRepository.GetAllAsync(cancellationToken);
            var existingNames = new HashSet<string>(existing.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Permission>();
            foreach (var permissionDefinition in _modules.SelectMany(m => m.Permissions))
            {
                if (!existingNames.Contains(permissionDefinition.Name) && toAdd.All(p => p.Name != permissionDefinition.Name))
                {
                    toAdd.Add(new Permission(permissionDefinition.Name, permissionDefinition.Description));
                }
            }

            if (toAdd.Count > 0)
            {
                _logger.LogInformation("Syncing {Count} new permissions to database", toAdd.Count);
                foreach (var p in toAdd)
                {
                    await _permissionRepository.AddAsync(p, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Permissions already in sync");
            }

            await BackfillModulesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync permissions");
            throw;
        }
    }

    // Requires ModuleSyncService to have already run in the same startup pass so every discovered
    // module's Key resolves to a persisted Module row. Uses a tracked query
    // (GetPermissionsMissingModuleAsync, not GetAllAsync's AsNoTracking) and mutates in place --
    // no explicit Update() call -- so this can't collide with a Permission instance some other
    // seeding step already tracked earlier in the same shared startup DbContext.
    private async Task BackfillModulesAsync(CancellationToken cancellationToken)
    {
        var unassigned = await _permissionRepository.GetPermissionsMissingModuleAsync(cancellationToken);
        if (unassigned.Count == 0)
        {
            return;
        }

        var modulesByKey = (await _moduleRepository.GetAllAsync(cancellationToken))
            .ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        // Owning module for a given permission name -- each module declares its own permissions,
        // so the first (only) module that lists a name owns it. Built fresh from the currently
        // discovered modules rather than a hand-maintained dictionary.
        var permissionOwner = _modules
            .SelectMany(m => m.Permissions.Select(p => new { p.Name, ModuleKey = m.Key }))
            .ToDictionary(x => x.Name, x => x.ModuleKey, StringComparer.OrdinalIgnoreCase);

        var updated = 0;
        foreach (var permission in unassigned)
        {
            if (permissionOwner.TryGetValue(permission.Name, out var moduleKey) &&
                modulesByKey.TryGetValue(moduleKey, out var module))
            {
                permission.AssignModule(module.Id);
                updated++;
            }
            else
            {
                _logger.LogWarning("Permission {Permission} is not declared by any discovered module", permission.Name);
            }
        }

        if (updated > 0)
        {
            _logger.LogInformation("Backfilled ModuleId on {Count} permissions", updated);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
