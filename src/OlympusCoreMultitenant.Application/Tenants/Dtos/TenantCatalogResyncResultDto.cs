namespace OlympusCoreMultitenant.Application.Tenants.Dtos;

public sealed record TenantCatalogResyncResultDto(int ResyncedCount, int SkippedCount, IReadOnlyList<long> ResyncedTenantIds);
