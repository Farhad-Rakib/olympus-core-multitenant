namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

public sealed record TenantRegistrationLinkDto(string Token, DateTime ExpiresAtUtc);
