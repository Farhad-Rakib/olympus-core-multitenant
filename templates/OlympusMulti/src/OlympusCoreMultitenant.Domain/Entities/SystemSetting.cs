using OlympusCoreMultitenant.Domain.Common;

namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
