namespace OlympusCoreMultitenant.Application.SystemSettings.Dtos;

public sealed record SystemSettingDto(
    long Id,
    string Key,
    string Value,
    string? Description
);
