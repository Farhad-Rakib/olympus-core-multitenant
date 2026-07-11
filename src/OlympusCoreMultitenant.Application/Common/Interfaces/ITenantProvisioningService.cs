using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface ITenantProvisioningService
{
    Task ProvisionAsync(Tenant tenant, IReadOnlyList<string>? moduleKeys = null, CancellationToken cancellationToken = default);
}
