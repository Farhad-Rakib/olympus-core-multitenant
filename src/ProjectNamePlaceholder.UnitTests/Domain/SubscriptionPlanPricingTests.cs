using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.UnitTests.Domain;

public class SubscriptionPlanPricingTests
{
    private static SubscriptionPlan Plan() => new("pro", "Pro", "Pro plan", 25);

    [Fact]
    public void A_new_plan_is_unpriced_and_not_on_a_billing_cycle()
    {
        var plan = Plan();

        Assert.Equal(0m, plan.Price);
        Assert.Equal(BillingInterval.None, plan.BillingInterval);
        Assert.Null(plan.ExternalPriceId);
    }

    [Fact]
    public void Pricing_is_stored_with_the_currency_normalised()
    {
        var plan = Plan();

        plan.SetPricing(49.99m, "usd", BillingInterval.Monthly, "price_123");

        Assert.Equal(49.99m, plan.Price);
        Assert.Equal("USD", plan.Currency);
        Assert.Equal(BillingInterval.Monthly, plan.BillingInterval);
        Assert.Equal("price_123", plan.ExternalPriceId);
    }

    [Fact]
    public void A_recurring_plan_cannot_be_free()
    {
        var plan = Plan();

        // Otherwise a tenant is put on a billing cycle that will never charge, and the licence is
        // silently extended forever.
        Assert.Throws<ArgumentException>(() => plan.SetPricing(0m, "USD", BillingInterval.Monthly, null));
    }

    [Fact]
    public void A_plan_with_no_billing_cycle_may_be_free()
    {
        var plan = Plan();

        plan.SetPricing(0m, "USD", BillingInterval.None, null);

        Assert.Equal(0m, plan.Price);
    }

    [Fact]
    public void A_negative_price_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Plan().SetPricing(-1m, "USD", BillingInterval.None, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    public void Currency_must_be_a_three_letter_code(string currency)
    {
        Assert.Throws<ArgumentException>(() => Plan().SetPricing(10m, currency, BillingInterval.Monthly, null));
    }

    [Fact]
    public void A_blank_external_price_id_is_stored_as_null()
    {
        var plan = Plan();

        // Null and "" must not both exist, or the unique index on ExternalPriceId collides across
        // every plan that has not been linked to the provider yet.
        plan.SetPricing(10m, "USD", BillingInterval.Monthly, "   ");

        Assert.Null(plan.ExternalPriceId);
    }

    [Fact]
    public void Renaming_a_plan_leaves_its_pricing_untouched()
    {
        var plan = Plan();
        plan.SetPricing(49m, "EUR", BillingInterval.Yearly, "price_abc");

        plan.Update("Pro Plus", "More of it", 50);

        Assert.Equal(49m, plan.Price);
        Assert.Equal("EUR", plan.Currency);
        Assert.Equal(BillingInterval.Yearly, plan.BillingInterval);
    }
}
