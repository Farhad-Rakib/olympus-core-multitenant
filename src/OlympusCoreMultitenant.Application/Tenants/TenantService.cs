using System.Security.Cryptography;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Common.Interfaces.Security;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Application.Tenants.Dtos;
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
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

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
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher)
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
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
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

        var adminUser = new User(pending.PendingFullName!, pending.PendingEmail!, pending.PendingPasswordHash!);

        var adminRole = await _roleRepository.GetByNameAsync(SystemRoles.Admin, cancellationToken)
            ?? throw new AppException("Admin role is missing for this tenant.", 500);

        adminUser.SetRoles(new[]
        {
            new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id, User = adminUser, Role = adminRole }
        });

        await _userRepository.AddAsync(adminUser, cancellationToken);

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

    private static TenantDto MapTenant(Tenant tenant) =>
        new(tenant.Id, tenant.Slug, tenant.Name, tenant.IsActive, tenant.SubscriptionPlanId, tenant.IsProvisioned);
}
