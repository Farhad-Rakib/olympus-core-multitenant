using ProjectNamePlaceholder.Application.Tenants.Dtos;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Tenants;

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateAsync(long id, UpdateTenantRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task ProvisionAsync(long tenantId, IReadOnlyList<string>? moduleKeys = null, long? subscriptionPlanId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModuleToggleDto>> GetModuleEntitlementsAsync(long tenantId, CancellationToken cancellationToken = default);
    Task EnableModuleAsync(long tenantId, long moduleId, CancellationToken cancellationToken = default);
    Task DisableModuleAsync(long tenantId, long moduleId, CancellationToken cancellationToken = default);
    Task AssignSubscriptionPlanAsync(long tenantId, long subscriptionPlanId, CancellationToken cancellationToken = default);
    Task<TenantRegistrationLinkDto> GenerateRegistrationLinkAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<TenantRegistrationInfoDto> GetRegistrationInfoAsync(string token, CancellationToken cancellationToken = default);
    Task SubmitRegistrationAsync(SubmitTenantRegistrationRequestDto request, CancellationToken cancellationToken = default);
    Task<PendingTenantRegistrationDto?> GetPendingRegistrationAsync(long tenantId, CancellationToken cancellationToken = default);
    Task ApproveRegistrationAsync(long tenantId, long? subscriptionPlanId = null, CancellationToken cancellationToken = default);
    Task ReprovisionCatalogAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<TenantCatalogResyncResultDto> ReprovisionAllCatalogsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantDomainDto>> GetDomainsAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<TenantDomainDto> AddDomainAsync(long tenantId, string domain, CancellationToken cancellationToken = default);
    Task RemoveDomainAsync(long tenantId, long domainId, CancellationToken cancellationToken = default);
    Task<TenantExportDto> ExportAsync(long tenantId, CancellationToken cancellationToken = default);

    // confirmationSlug must equal the tenant's own slug -- a deliberate "type the name to confirm"
    // gate against an accidental irreversible call. Also requires the tenant to already be disabled
    // and past its cooling-off period; see TenantOffboardingOptions.PurgeCoolingOffDays.
    Task PurgeAsync(long tenantId, string confirmationSlug, CancellationToken cancellationToken = default);
    Task<TenantOverviewDto> GetOverviewAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<LicenseStatus> GetLicenseStatusAsync(long tenantId, CancellationToken cancellationToken = default);
    Task StartTrialAsync(long tenantId, DateTime trialEndsAtUtc, CancellationToken cancellationToken = default);
    Task SetLicenseExpiryAsync(long tenantId, DateTime? licenseExpiresAtUtc, CancellationToken cancellationToken = default);
    Task SuspendLicenseAsync(long tenantId, CancellationToken cancellationToken = default);
    Task UnsuspendLicenseAsync(long tenantId, CancellationToken cancellationToken = default);
}
