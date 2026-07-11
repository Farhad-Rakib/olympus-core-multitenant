namespace OlympusCoreMultitenant.Application.Users.Dtos;

public sealed record UpdateProfileRequestDto(
    string FullName,
    string Email,
    string? ProfileImageUrl
);
