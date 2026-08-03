namespace OlympusCoreMultitenant.Application.Roles.Dtos;

public sealed record UpdateRolePermissionsRequestDto(
    IReadOnlyList<long> PermissionIds);