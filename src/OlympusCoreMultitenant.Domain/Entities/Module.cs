using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class Module : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ModuleKind Kind { get; private set; }

    private Module()
    {
    }

    public Module(string key, string name, string description, ModuleKind kind)
    {
        Key = !string.IsNullOrWhiteSpace(key)
            ? key.Trim().ToLowerInvariant()
            : throw new ArgumentException("Module key is required.", nameof(key));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Module name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;
        Kind = kind;
    }

    public void Update(string name, string description)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Module name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;
    }
}
