namespace OlympusCoreMultitenant.Application.Tenants.Dtos;

public sealed record TenantDto(long Id, string Slug, string Name, bool IsActive, long? SubscriptionPlanId);
