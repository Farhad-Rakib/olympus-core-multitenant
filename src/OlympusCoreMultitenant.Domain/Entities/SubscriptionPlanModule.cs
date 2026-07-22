namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class SubscriptionPlanModule
{
    public long SubscriptionPlanId { get; set; }
    public long ModuleId { get; set; }

    public Module Module { get; set; } = null!;
}
