using OlympusCoreMultitenant.Application.Tenants.Dtos;

namespace OlympusCoreMultitenant.Application.Tenants;

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateAsync(long id, UpdateTenantRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task ProvisionAsync(long tenantId, IReadOnlyList<string>? moduleKeys = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModuleToggleDto>> GetModuleEntitlementsAsync(long tenantId, CancellationToken cancellationToken = default);
    Task EnableModuleAsync(long tenantId, long moduleId, CancellationToken cancellationToken = default);
    Task DisableModuleAsync(long tenantId, long moduleId, CancellationToken cancellationToken = default);
}
