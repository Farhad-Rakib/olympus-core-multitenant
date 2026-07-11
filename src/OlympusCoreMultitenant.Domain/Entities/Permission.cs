namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class Permission : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public long? ModuleId { get; private set; }
    public Module? Module { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Permission()
    {
    }

    public Permission(string name, string description)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Permission name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;
    }

    public void Update(string name, string description)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Permission name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;
    }

    public void AssignModule(long moduleId)
    {
        ModuleId = moduleId;
    }
}
