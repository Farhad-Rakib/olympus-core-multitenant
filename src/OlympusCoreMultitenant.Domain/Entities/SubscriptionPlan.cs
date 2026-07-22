namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class SubscriptionPlan : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int MaxUsers { get; private set; }
    public bool IsActive { get; private set; } = true;

    private SubscriptionPlan()
    {
    }

    public SubscriptionPlan(string key, string name, string description, int maxUsers)
    {
        Key = !string.IsNullOrWhiteSpace(key)
            ? key.Trim().ToLowerInvariant()
            : throw new ArgumentException("Subscription plan key is required.", nameof(key));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Subscription plan name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;

        MaxUsers = maxUsers > 0
            ? maxUsers
            : throw new ArgumentException("Max users must be greater than zero.", nameof(maxUsers));
    }

    public void Update(string name, string description, int maxUsers)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Subscription plan name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;

        MaxUsers = maxUsers > 0
            ? maxUsers
            : throw new ArgumentException("Max users must be greater than zero.", nameof(maxUsers));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
