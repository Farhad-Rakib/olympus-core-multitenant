namespace OlympusCoreMultitenant.Application.Tenants.Dtos;

public sealed record ModuleToggleDto(
    long Id,
    string Key,
    string Name,
    string Description,
    bool IsEnabled);
