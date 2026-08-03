namespace OlympusCoreMultitenant.Application.Auth.Dtos;

public sealed record LoginRequestDto(string Email, string Password, string? TenantSlug = null);
