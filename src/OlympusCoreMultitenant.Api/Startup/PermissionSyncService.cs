using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Api.Startup;

public sealed class PermissionSyncService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(
        IPermissionRepository permissionRepository,
        IModuleRepository moduleRepository,
        IUnitOfWork unitOfWork,
        ILogger<PermissionSyncService> logger)
    {
        _permissionRepository = permissionRepository;
        _moduleRepository = moduleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _permissionRepository.GetAllAsync(cancellationToken);
            var existingNames = new HashSet<string>(existing.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Permission>();
            foreach (var name in Permissions.All)
            {
                if (!existingNames.Contains(name))
                {
                    toAdd.Add(new Permission(name, string.Empty));
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

    // Requires ModuleSyncService to have already run in the same startup pass so every key in
    // Modules.PermissionModuleMap resolves to a persisted Module row. Uses a tracked query
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

        var updated = 0;
        foreach (var permission in unassigned)
        {
            if (Modules.PermissionModuleMap.TryGetValue(permission.Name, out var moduleKey) &&
                modulesByKey.TryGetValue(moduleKey, out var module))
            {
                permission.AssignModule(module.Id);
                updated++;
            }
            else
            {
                _logger.LogWarning("Permission {Permission} has no module mapping in Modules.PermissionModuleMap", permission.Name);
            }
        }

        if (updated > 0)
        {
            _logger.LogInformation("Backfilled ModuleId on {Count} permissions", updated);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
