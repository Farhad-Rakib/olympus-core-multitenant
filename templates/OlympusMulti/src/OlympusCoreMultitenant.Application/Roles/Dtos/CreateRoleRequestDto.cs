namespace OlympusCoreMultitenant.Application.Roles.Dtos;

public sealed record CreateRoleRequestDto(
    string Name,
    string Description);