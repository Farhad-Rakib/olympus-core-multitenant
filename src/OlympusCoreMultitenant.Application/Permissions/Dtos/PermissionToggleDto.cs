namespace OlympusCoreMultitenant.Application.Permissions.Dtos;

public sealed record PermissionToggleDto(
    long Id,
    string Name,
    string Description,
    bool IsAssigned);
