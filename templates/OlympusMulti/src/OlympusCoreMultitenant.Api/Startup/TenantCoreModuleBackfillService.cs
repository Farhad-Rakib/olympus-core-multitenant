using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Api.Startup;

// One-time-per-environment fix-up for tenants provisioned before Core became a revocable
// TenantModule entitlement instead of an unconditional pass. Idempotent and cheap once every
// tenant has caught up (matches the existing ModuleSyncService/PermissionSyncService idiom).
public sealed class TenantCoreModuleBackfillService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ITenantModuleRepository _tenantModuleRepository;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantCoreModuleBackfillService> _logger;

    public TenantCoreModuleBackfillService(
        ITenantRepository tenantRepository,
        IModuleRepository moduleRepository,
        ITenantModuleRepository tenantModuleRepository,
        ICurrentTenantService currentTenantService,
        IUnitOfWork unitOfWork,
        ILogger<TenantCoreModuleBackfillService> logger)
    {
        _tenantRepository = tenantRepository;
        _moduleRepository = moduleRepository;
        _tenantModuleRepository = tenantModuleRepository;
        _currentTenantService = currentTenantService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task BackfillAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var coreModules = (await _moduleRepository.GetAllAsync(cancellationToken))
                .Where(module => module.Kind == ModuleKind.Core)
                .ToList();

            if (coreModules.Count == 0)
            {
                return;
            }

            var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
            var backfilledTenantCount = 0;

            foreach (var tenant in tenants)
            {
                // Scoped per tenant deliberately: ApplyTenantStamping stamps TenantId on Added
                // entities using whatever tenant context is ambient when SaveChangesAsync runs, not
                // when Add() was called. SaveChangesAsync must happen inside this tenant's own scope,
                // before moving to the next tenant, or the stamping breaks (or throws).
                using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

                var entitledModuleIds = (await _tenantModuleRepository.GetEntitledModuleIdsAsync(cancellationToken)).ToHashSet();
                var addedAny = false;

                foreach (var module in coreModules)
                {
                    if (!entitledModuleIds.Contains(module.Id))
                    {
                        await _tenantModuleRepository.AddAsync(new TenantModule { ModuleId = module.Id }, cancellationToken);
                        addedAny = true;
                    }
                }

                if (addedAny)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    backfilledTenantCount++;
                }
            }

            if (backfilledTenantCount > 0)
            {
                _logger.LogInformation("Backfilled Core module entitlement for {Count} tenant(s)", backfilledTenantCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backfill Core module entitlements");
            throw;
        }
    }
}
