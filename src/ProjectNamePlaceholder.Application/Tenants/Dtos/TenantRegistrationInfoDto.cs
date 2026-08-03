namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

public sealed record TenantRegistrationInfoDto(string TenantName, string TenantSlug, bool AlreadySubmitted);
