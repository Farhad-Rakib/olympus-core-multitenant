using OlympusCoreMultitenant.Application.Tenants.Dtos;

namespace OlympusCoreMultitenant.Application.Tenants;

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
}
