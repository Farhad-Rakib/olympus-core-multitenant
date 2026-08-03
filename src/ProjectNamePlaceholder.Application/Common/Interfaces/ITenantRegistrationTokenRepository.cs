using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ITenantRegistrationTokenRepository : IRepository<TenantRegistrationToken>
{
    Task<TenantRegistrationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<TenantRegistrationToken?> GetPendingByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);
}
