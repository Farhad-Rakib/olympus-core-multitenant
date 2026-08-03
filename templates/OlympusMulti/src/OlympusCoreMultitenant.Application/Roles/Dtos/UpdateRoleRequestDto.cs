namespace OlympusCoreMultitenant.Application.Roles.Dtos;

public sealed record UpdateRoleRequestDto(
    string Name,
    string Description);