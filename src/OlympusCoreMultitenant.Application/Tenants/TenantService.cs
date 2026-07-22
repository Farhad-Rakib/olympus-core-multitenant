using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Application.Tenants.Dtos;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.Tenants;

public sealed class TenantService : ITenantService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ITenantModuleRepository _tenantModuleRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ISubscriptionPlanModuleRepository _subscriptionPlanModuleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ICurrentTenantService _currentTenantService;

    public TenantService(
        IRepository<Tenant> tenantRepository,
        IModuleRepository moduleRepository,
        ITenantModuleRepository tenantModuleRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ISubscriptionPlanModuleRepository subscriptionPlanModuleRepository,
        IUnitOfWork unitOfWork,
        ITenantProvisioningService tenantProvisioningService,
        ICurrentTenantService currentTenantService)
    {
        _tenantRepository = tenantRepository;
        _moduleRepository = moduleRepository;
        _tenantModuleRepository = tenantModuleRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _subscriptionPlanModuleRepository = subscriptionPlanModuleRepository;
        _unitOfWork = unitOfWork;
        _tenantProvisioningService = tenantProvisioningService;
        _currentTenantService = currentTenantService;
    }

    public async Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        return tenants.Select(MapTenant).ToList();
    }

    public async Task<TenantDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        return tenant is null ? null : MapTenant(tenant);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var existing = await _tenantRepository.GetAllAsync(cancellationToken);
        if (existing.Any(t => t.Slug == slug))
        {
            throw new AppException("A tenant with this slug already exists.", 409);
        }

        var tenant = new Tenant(slug, request.Name);
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapTenant(tenant);
    }

    public async Task<TenantDto> UpdateAsync(long id, UpdateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        tenant.Update(request.Name);
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapTenant(tenant);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        tenant.Disable();
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ProvisionAsync(long tenantId, IReadOnlyList<string>? moduleKeys = null, long? subscriptionPlanId = null, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        var effectiveModuleKeys = moduleKeys;
        if (subscriptionPlanId is not null)
        {
            var plan = await _subscriptionPlanRepository.GetByIdAsync(subscriptionPlanId.Value, cancellationToken)
                ?? throw new AppException("Subscription plan not found.", 404);

            var planModuleIds = (await _subscriptionPlanModuleRepository.GetModuleIdsForPlanAsync(plan.Id, cancellationToken)).ToHashSet();
            var allModules = await _moduleRepository.GetAllAsync(cancellationToken);
            effectiveModuleKeys = allModules
                .Where(module => planModuleIds.Contains(module.Id))
                .Select(module => module.Key)
                .ToList();
        }

        await _tenantProvisioningService.ProvisionAsync(tenant, effectiveModuleKeys, cancellationToken);

        if (subscriptionPlanId is not null)
        {
            tenant.AssignSubscriptionPlan(subscriptionPlanId);
            _tenantRepository.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    // Full resync (add + remove) for changing an already-provisioned tenant's plan later, unlike
    // TenantProvisioningService.SeedModuleEntitlementsAsync which stays add-only since it also runs
    // on every app boot for the bootstrap "default" tenant -- making that path destructive too would
    // be an unrelated risk. Core is always kept regardless of which plan is assigned.
    public async Task AssignSubscriptionPlanAsync(long tenantId, long subscriptionPlanId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        var plan = await _subscriptionPlanRepository.GetByIdAsync(subscriptionPlanId, cancellationToken)
            ?? throw new AppException("Subscription plan not found.", 404);

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        var targetModuleIds = (await _subscriptionPlanModuleRepository.GetModuleIdsForPlanAsync(plan.Id, cancellationToken)).ToHashSet();

        var coreModule = await _moduleRepository.GetByKeyAsync(Modules.Core, cancellationToken);
        if (coreModule is not null)
        {
            targetModuleIds.Add(coreModule.Id);
        }

        var currentModuleIds = (await _tenantModuleRepository.GetEntitledModuleIdsAsync(cancellationToken)).ToHashSet();

        foreach (var moduleId in targetModuleIds.Except(currentModuleIds))
        {
            await _tenantModuleRepository.AddAsync(new TenantModule { ModuleId = moduleId }, cancellationToken);
        }

        foreach (var moduleId in currentModuleIds.Except(targetModuleIds))
        {
            var existing = await _tenantModuleRepository.GetAsync(moduleId, cancellationToken);
            if (existing is not null)
            {
                _tenantModuleRepository.Remove(existing);
            }
        }

        tenant.AssignSubscriptionPlan(subscriptionPlanId);
        _tenantRepository.Update(tenant);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModuleToggleDto>> GetModuleEntitlementsAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        var allModules = await _moduleRepository.GetAllAsync(cancellationToken);
        var entitledModuleIds = (await _tenantModuleRepository.GetEntitledModuleIdsAsync(cancellationToken)).ToHashSet();

        return allModules
            .Where(module => module.Kind != ModuleKind.System)
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .Select(module => new ModuleToggleDto(module.Id, module.Key, module.Name, module.Description, entitledModuleIds.Contains(module.Id)))
            .ToList();
    }

    public async Task EnableModuleAsync(long tenantId, long moduleId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        var module = await _moduleRepository.GetByIdAsync(moduleId, cancellationToken)
            ?? throw new AppException("Module not found.", 404);

        if (module.Kind == ModuleKind.System)
        {
            throw new AppException("System-kind modules can never be entitled to a tenant.", 400);
        }

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        var existing = await _tenantModuleRepository.GetAsync(moduleId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await _tenantModuleRepository.AddAsync(new TenantModule { ModuleId = moduleId }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableModuleAsync(long tenantId, long moduleId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        var existing = await _tenantModuleRepository.GetAsync(moduleId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _tenantModuleRepository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static TenantDto MapTenant(Tenant tenant) =>
        new(tenant.Id, tenant.Slug, tenant.Name, tenant.IsActive, tenant.SubscriptionPlanId);
}
