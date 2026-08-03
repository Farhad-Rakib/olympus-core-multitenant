namespace OlympusCoreMultitenant.Application.Users.Dtos;

public sealed record UpdateUserRolesRequestDto(
    IReadOnlyList<long> RoleIds);