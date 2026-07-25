using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface ITenantRegistrationTokenRepository : IRepository<TenantRegistrationToken>
{
    Task<TenantRegistrationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<TenantRegistrationToken?> GetPendingByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);
}
