namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    long? TenantId { get; }
    string? TenantSlug { get; }

    void Set(long tenantId, string tenantSlug);

    IDisposable BeginScope(long tenantId, string tenantSlug);

    void Clear();
}
