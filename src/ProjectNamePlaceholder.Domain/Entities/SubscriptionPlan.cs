using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class SubscriptionPlan : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int MaxUsers { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Stored as decimal, never double: money must not carry binary floating-point rounding error.
    // The unit is whole currency (e.g. 49.00 USD), not minor units -- providers differ on that, so
    // the conversion belongs in the gateway adapter rather than being baked into the domain.
    public decimal Price { get; private set; }

    // ISO 4217. Per-plan rather than global so the same platform can sell in more than one currency.
    public string Currency { get; private set; } = "USD";

    public BillingInterval BillingInterval { get; private set; } = BillingInterval.None;

    // The plan's identifier in the payment provider (e.g. a Stripe price id). Kept here so the
    // mapping lives with the plan, and nullable because a plan can exist before it is sold.
    public string? ExternalPriceId { get; private set; }

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

    // Pricing is set separately from Update(): renaming a plan is routine and safe, while changing
    // what it costs is a commercial decision that should be an explicit, separately auditable act.
    public void SetPricing(decimal price, string currency, BillingInterval billingInterval, string? externalPriceId)
    {
        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative.", nameof(price));
        }

        if (billingInterval != BillingInterval.None && price <= 0)
        {
            throw new ArgumentException("A recurring plan must have a price above zero.", nameof(price));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        }

        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        BillingInterval = billingInterval;
        ExternalPriceId = string.IsNullOrWhiteSpace(externalPriceId) ? null : externalPriceId.Trim();
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
