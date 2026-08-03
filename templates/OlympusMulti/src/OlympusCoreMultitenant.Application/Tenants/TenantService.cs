using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OlympusCoreMultitenant.Application.Common.Configuration;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Common.Interfaces.Security;
using OlympusCoreMultitenant.Application.Integration;
using OlympusCoreMultitenant.Application.Licensing;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Application.Tenants.Dtos;
using OlympusCoreMultitenant.Application.Tenants.Events;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.Tenants;

public sealed class TenantService : ITenantService
{
    private const int RegistrationTokenExpiryDays = 7;

    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ITenantModuleRepository _tenantModuleRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ISubscriptionPlanModuleRepository _subscriptionPlanModuleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ITenantRegistrationTokenRepository _tenantRegistrationTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantDomainRepository _tenantDomainRepository;
    private readonly ITenantOffboardingService _tenantOffboardingService;
    private readonly TenantOffboardingOptions _offboardingOptions;
    private readonly ILicenseService _licenseService;
    private readonly IEventBus _eventBus;

    public TenantService(
        IRepository<Tenant> tenantRepository,
        IModuleRepository moduleRepository,
        ITenantModuleRepository tenantModuleRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ISubscriptionPlanModuleRepository subscriptionPlanModuleRepository,
        IUnitOfWork unitOfWork,
        ITenantProvisioningService tenantProvisioningService,
        ICurrentTenantService currentTenantService,
        ITenantRegistrationTokenRepository tenantRegistrationTokenRepository,
        IPasswordHasher passwordHasher,
        ITenantDomainRepository tenantDomainRepository,
        ITenantOffboardingService tenantOffboardingService,
        IOptions<TenantOffboardingOptions> offboardingOptions,
        ILicenseService licenseService,
        IEventBus eventBus)
    {
        _tenantRepository = tenantRepository;
        _moduleRepository = moduleRepository;
        _tenantModuleRepository = tenantModuleRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _subscriptionPlanModuleRepository = subscriptionPlanModuleRepository;
        _unitOfWork = unitOfWork;
        _tenantProvisioningService = tenantProvisioningService;
        _currentTenantService = currentTenantService;
        _tenantRegistrationTokenRepository = tenantRegistrationTokenRepository;
        _passwordHasher = passwordHasher;
        _tenantDomainRepository = tenantDomainRepository;
        _tenantOffboardingService = tenantOffboardingService;
        _offboardingOptions = offboardingOptions.Value;
        _licenseService = licenseService;
        _eventBus = eventBus;
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

    private async Task<IReadOnlyList<string>?> ResolveModuleKeysForPlanAsync(IReadOnlyList<string>? explicitModuleKeys, long? subscriptionPlanId, CancellationToken cancellationToken)
    {
        if (subscriptionPlanId is null)
        {
            return explicitModuleKeys;
        }

        var plan = await _subscriptionPlanRepository.GetByIdAsync(subscriptionPlanId.Value, cancellationToken)
            ?? throw new AppException("Subscription plan not found.", 404);

        var planModuleIds = (await _subscriptionPlanModuleRepository.GetModuleIdsForPlanAsync(plan.Id, cancellationToken)).ToHashSet();
        var allModules = await _moduleRepository.GetAllAsync(cancellationToken);
        return allModules
            .Where(module => planModuleIds.Contains(module.Id))
            .Select(module => module.Key)
            .ToList();
    }

    public async Task ProvisionAsync(long tenantId, IReadOnlyList<string>? moduleKeys = null, long? subscriptionPlanId = null, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        var effectiveModuleKeys = await ResolveModuleKeysForPlanAsync(moduleKeys, subscriptionPlanId, cancellationToken);

        await _tenantProvisioningService.ProvisionAsync(tenant, effectiveModuleKeys, cancellationToken);

        if (subscriptionPlanId is not null)
        {
            tenant.AssignSubscriptionPlan(subscriptionPlanId);
        }

        tenant.MarkProvisioned();
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Full resync (add + remove) for changing an already-provisioned tenant's plan later, unlike
    // TenantProvisioningService.SeedModuleEntitlementsAsync which stays add-only since it also runs
    // on every app boot for the bootstrap "default" tenant -- making that path destructive too would
    // be an unrelated risk. Core is always kept regardless of which plan is assigned.
    public async Task AssignSubscriptionPlanAsync(long tenantId, long subscriptionPlanId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        if (!tenant.IsProvisioned)
        {
            throw new AppException("Tenant must be provisioned before a subscription plan can be assigned.", 409);
        }

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

    // Only meaningful before the tenant is provisioned. Sharing this link never grants access by
    // itself -- the invitee can only submit a request (see SubmitRegistrationAsync); the tenant
    // stays unprovisioned and unusable until a super admin reviews and approves it, or activates
    // the tenant directly and ignores the submission entirely.
    public async Task<TenantRegistrationLinkDto> GenerateRegistrationLinkAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        if (tenant.IsProvisioned)
        {
            throw new AppException("Tenant is already provisioned.", 409);
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.AddDays(RegistrationTokenExpiryDays);

        await _tenantRegistrationTokenRepository.AddAsync(new TenantRegistrationToken(tenant.Id, token, expiresAtUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TenantRegistrationLinkDto(token, expiresAtUtc);
    }

    public async Task<TenantRegistrationInfoDto> GetRegistrationInfoAsync(string token, CancellationToken cancellationToken = default)
    {
        var registrationToken = await _tenantRegistrationTokenRepository.GetByTokenAsync(token, cancellationToken);
        if (registrationToken is null || registrationToken.UsedAtUtc is not null || registrationToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AppException("This registration link is invalid or has expired.", 400);
        }

        var tenant = await _tenantRepository.GetByIdAsync(registrationToken.TenantId, cancellationToken)
            ?? throw new AppException("This registration link is invalid or has expired.", 400);

        if (tenant.IsProvisioned)
        {
            throw new AppException("This tenant has already been activated.", 409);
        }

        return new TenantRegistrationInfoDto(tenant.Name, tenant.Slug, registrationToken.SubmittedAtUtc is not null);
    }

    // Deliberately does NOT provision the tenant or create a usable login -- it only records what
    // the invitee entered as a pending request. See ApproveRegistrationAsync for the step that
    // actually grants access.
    public async Task SubmitRegistrationAsync(SubmitTenantRegistrationRequestDto request, CancellationToken cancellationToken = default)
    {
        var registrationToken = await _tenantRegistrationTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (registrationToken is null || !registrationToken.CanBeSubmitted)
        {
            throw new AppException("This registration link is invalid, has expired, or has already been submitted.", 400);
        }

        var tenant = await _tenantRepository.GetByIdAsync(registrationToken.TenantId, cancellationToken)
            ?? throw new AppException("This registration link is invalid or has expired.", 400);

        if (tenant.IsProvisioned)
        {
            throw new AppException("This tenant has already been activated.", 409);
        }

        var hashedPassword = _passwordHasher.Hash(request.Password);
        registrationToken.Submit(request.FullName, request.Email, hashedPassword);
        _tenantRegistrationTokenRepository.Update(registrationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PendingTenantRegistrationDto?> GetPendingRegistrationAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var pending = await _tenantRegistrationTokenRepository.GetPendingByTenantIdAsync(tenantId, cancellationToken);
        return pending is null
            ? null
            : new PendingTenantRegistrationDto(pending.PendingFullName!, pending.PendingEmail!, pending.SubmittedAtUtc!.Value);
    }

    // The super-admin-driven counterpart to a submitted request: provisions the tenant (same path
    // as ProvisionAsync) and creates the Admin user from what the invitee submitted, rather than
    // the generic seeded account ProvisionAsync/Activate would otherwise leave behind.
    public async Task ApproveRegistrationAsync(long tenantId, long? subscriptionPlanId = null, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        if (tenant.IsProvisioned)
        {
            throw new AppException("Tenant is already provisioned.", 409);
        }

        var pending = await _tenantRegistrationTokenRepository.GetPendingByTenantIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("No pending registration request for this tenant.", 404);

        var effectiveModuleKeys = await ResolveModuleKeysForPlanAsync(explicitModuleKeys: null, subscriptionPlanId, cancellationToken);

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        await _tenantProvisioningService.ProvisionAsync(tenant, effectiveModuleKeys, cancellationToken);

        // Publishing instead of constructing the admin User/Role directly: this flow doesn't need
        // to know how "create an admin account" is implemented, only that the fact happened. See
        // CreateAdminUserOnTenantApprovedHandler for the reaction. Published before the
        // SaveChangesAsync below (not after) -- the handler only tracks the new entities via the
        // same scoped DbContext, so they still commit atomically with the rest of this method's
        // work, exactly as when this was one inline block.
        await _eventBus.PublishAsync(
            new TenantApprovedIntegrationEvent(tenant.Id, tenant.Slug, pending.PendingFullName!, pending.PendingEmail!, pending.PendingPasswordHash!),
            cancellationToken);

        pending.MarkUsed();
        _tenantRegistrationTokenRepository.Update(pending);

        if (subscriptionPlanId is not null)
        {
            tenant.AssignSubscriptionPlan(subscriptionPlanId);
        }

        tenant.MarkProvisioned();
        _tenantRepository.Update(tenant);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Catches an already-provisioned tenant up on RbacSeeder/DefaultMenuSeeder defaults added after
    // it was first provisioned. Never touches module entitlements -- those change only through
    // EnableModule/DisableModule/AssignSubscriptionPlan, not by silently re-adding what an admin
    // deliberately revoked.
    public async Task ReprovisionCatalogAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        if (!tenant.IsProvisioned)
        {
            throw new AppException("Tenant must be provisioned before its catalog can be resynced.", 409);
        }

        await _tenantProvisioningService.ResyncCatalogAsync(tenant, cancellationToken);
    }

    public async Task<TenantCatalogResyncResultDto> ReprovisionAllCatalogsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        var resyncedIds = new List<long>();
        var skippedCount = 0;

        foreach (var tenant in tenants)
        {
            if (!tenant.IsProvisioned)
            {
                skippedCount++;
                continue;
            }

            await _tenantProvisioningService.ResyncCatalogAsync(tenant, cancellationToken);
            resyncedIds.Add(tenant.Id);
        }

        return new TenantCatalogResyncResultDto(resyncedIds.Count, skippedCount, resyncedIds);
    }

    public async Task<IReadOnlyList<TenantDomainDto>> GetDomainsAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var domains = await _tenantDomainRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        return domains.Select(d => new TenantDomainDto(d.Id, d.Domain)).ToList();
    }

    public async Task<TenantDomainDto> AddDomainAsync(long tenantId, string domain, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        var normalized = domain.Trim().ToLowerInvariant();
        var existing = await _tenantDomainRepository.GetByDomainAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            throw new AppException("This domain is already assigned to a tenant.", 409);
        }

        var tenantDomain = new TenantDomain(tenant.Id, normalized);
        await _tenantDomainRepository.AddAsync(tenantDomain, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TenantDomainDto(tenantDomain.Id, tenantDomain.Domain);
    }

    public async Task RemoveDomainAsync(long tenantId, long domainId, CancellationToken cancellationToken = default)
    {
        var tenantDomain = await _tenantDomainRepository.GetByIdAsync(domainId, cancellationToken);
        if (tenantDomain is null || tenantDomain.TenantId != tenantId)
        {
            return;
        }

        _tenantDomainRepository.Delete(tenantDomain);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantExportDto> ExportAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        return await _tenantOffboardingService.ExportAsync(tenant, cancellationToken);
    }

    public async Task PurgeAsync(long tenantId, string confirmationSlug, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        if (tenant.IsActive)
        {
            throw new AppException("Tenant must be disabled before it can be purged.", 409);
        }

        var coolingOffPeriod = TimeSpan.FromDays(_offboardingOptions.PurgeCoolingOffDays);
        if (tenant.DisabledAtUtc is null || DateTime.UtcNow - tenant.DisabledAtUtc.Value < coolingOffPeriod)
        {
            var remaining = tenant.DisabledAtUtc is null
                ? coolingOffPeriod
                : coolingOffPeriod - (DateTime.UtcNow - tenant.DisabledAtUtc.Value);
            throw new AppException(
                $"Tenant must remain disabled for {_offboardingOptions.PurgeCoolingOffDays} day(s) before it can be purged. " +
                $"Approximately {Math.Max(0, Math.Ceiling(remaining.TotalDays))} day(s) remaining.", 409);
        }

        if (!string.Equals(confirmationSlug?.Trim(), tenant.Slug, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("Confirmation slug does not match the tenant being purged.", 400);
        }

        await _tenantOffboardingService.PurgeAsync(tenant, cancellationToken);
    }

    public Task<LicenseStatus> GetLicenseStatusAsync(long tenantId, CancellationToken cancellationToken = default) =>
        _licenseService.GetStatusAsync(tenantId, cancellationToken);

    public async Task StartTrialAsync(long tenantId, DateTime trialEndsAtUtc, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        tenant.StartTrial(trialEndsAtUtc);
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetLicenseExpiryAsync(long tenantId, DateTime? licenseExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        tenant.SetLicenseExpiry(licenseExpiresAtUtc);
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SuspendLicenseAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        tenant.Suspend();
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnsuspendLicenseAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        tenant.Unsuspend();
        _tenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static TenantDto MapTenant(Tenant tenant) =>
        new(tenant.Id, tenant.Slug, tenant.Name, tenant.IsActive, tenant.SubscriptionPlanId, tenant.IsProvisioned);
}
