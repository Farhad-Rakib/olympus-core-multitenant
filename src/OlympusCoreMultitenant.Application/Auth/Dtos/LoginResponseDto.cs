namespace OlympusCoreMultitenant.Application.Auth.Dtos;

public sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc);
