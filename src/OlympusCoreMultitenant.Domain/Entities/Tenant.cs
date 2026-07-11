namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Tenant()
    {
    }

    public Tenant(string slug, string name)
    {
        Slug = !string.IsNullOrWhiteSpace(slug)
            ? slug.Trim().ToLowerInvariant()
            : throw new ArgumentException("Tenant slug is required.", nameof(slug));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Tenant name is required.", nameof(name));
    }

    public void Update(string name)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Tenant name is required.", nameof(name));
    }

    public void Disable()
    {
        IsActive = false;
    }

    public void Enable()
    {
        IsActive = true;
    }
}
