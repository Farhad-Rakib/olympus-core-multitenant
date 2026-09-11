using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Billing;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Application;

public class TenantBillingServiceTests
{
    // Stands in for a real provider adapter: records what it was asked for and hands back a URL.
    private sealed class FakeGateway : IPaymentGateway
    {
        public string Provider => "fake";
        public string? LastPlanKey { get; private set; }

        public Task<BillingEvent?> ParseWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default)
            => Task.FromResult<BillingEvent?>(null);

        public Task<string> CreateCheckoutUrlAsync(long tenantId, string planKey, CancellationToken cancellationToken = default)
        {
            LastPlanKey = planKey;
            return Task.FromResult($"https://checkout.example/{tenantId}/{planKey}");
        }

        public Task<string> CreateBillingPortalUrlAsync(long tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult($"https://portal.example/{tenantId}");
    }

    private sealed class Fixture
    {
        public FakeGateway Gateway { get; } = new();
        public FakeTenantRepository Tenants { get; } = new();
        public FakeSubscriptionPlanRepository Plans { get; } = new();
        public Tenant Tenant { get; }

        public Fixture(bool enabled = true, IPaymentGateway? gateway = null)
        {
            Tenant = Tenants.AddAsync(new Tenant("acme", "Acme Inc")).Result;
            Service = new TenantBillingService(
                gateway ?? Gateway,
                Tenants,
                Plans,
                Options.Create(new PaymentOptions { Enabled = enabled, Provider = (gateway ?? Gateway).Provider }));
        }

        public TenantBillingService Service { get; }

        public SubscriptionPlan AddPlan(string key, decimal price = 49m, BillingInterval interval = BillingInterval.Monthly, string? externalPriceId = "price_123", bool active = true)
        {
            var plan = new SubscriptionPlan(key, key.ToUpperInvariant(), "", 10);
            plan.SetPricing(price, "USD", interval, externalPriceId);
            if (!active) plan.Deactivate();
            return Plans.AddAsync(plan).Result;
        }
    }

    [Fact]
    public void Config_reports_checkout_unavailable_under_manual_provider()
    {
        var fixture = new Fixture(enabled: true, gateway: new ManualPaymentGateway());

        var config = fixture.Service.GetConfig();

        Assert.Equal("manual", config.Provider);
        Assert.False(config.CheckoutAvailable);
    }

    [Fact]
    public void Config_reports_checkout_unavailable_when_payment_disabled()
    {
        var fixture = new Fixture(enabled: false);

        Assert.False(fixture.Service.GetConfig().CheckoutAvailable);
    }

    [Fact]
    public async Task Checkout_refused_with_501_when_billing_is_not_enabled()
    {
        var fixture = new Fixture(enabled: false);
        fixture.AddPlan("pro");

        var ex = await Assert.ThrowsAsync<AppException>(() => fixture.Service.StartCheckoutAsync(fixture.Tenant.Id, "pro"));

        Assert.Equal(501, ex.StatusCode);
    }

    [Fact]
    public async Task Checkout_refused_for_unknown_plan()
    {
        var fixture = new Fixture();

        var ex = await Assert.ThrowsAsync<AppException>(() => fixture.Service.StartCheckoutAsync(fixture.Tenant.Id, "nope"));

        Assert.Equal(404, ex.StatusCode);
    }

    // A plan a platform admin manages by hand (no recurring price, or no provider price id) must
    // not reach the gateway -- the gateway would fail with a provider-specific error at best.
    [Theory]
    [InlineData(0, BillingInterval.None, "price_1")]
    [InlineData(49, BillingInterval.Monthly, null)]
    public async Task Checkout_refused_for_plan_that_is_not_sellable(decimal price, BillingInterval interval, string? externalPriceId)
    {
        var fixture = new Fixture();
        fixture.AddPlan("internal", price, interval, externalPriceId);

        var ex = await Assert.ThrowsAsync<AppException>(() => fixture.Service.StartCheckoutAsync(fixture.Tenant.Id, "internal"));

        Assert.Equal(409, ex.StatusCode);
        Assert.Null(fixture.Gateway.LastPlanKey);
    }

    [Fact]
    public async Task Checkout_refused_for_inactive_plan()
    {
        var fixture = new Fixture();
        fixture.AddPlan("old", active: false);

        var ex = await Assert.ThrowsAsync<AppException>(() => fixture.Service.StartCheckoutAsync(fixture.Tenant.Id, "old"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Checkout_normalises_the_plan_key_and_returns_the_gateway_url()
    {
        var fixture = new Fixture();
        fixture.AddPlan("pro");

        var result = await fixture.Service.StartCheckoutAsync(fixture.Tenant.Id, "  PRO ");

        Assert.Equal("pro", fixture.Gateway.LastPlanKey);
        Assert.Equal($"https://checkout.example/{fixture.Tenant.Id}/pro", result.Url);
    }

    [Fact]
    public async Task Portal_returns_the_gateway_url()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.OpenBillingPortalAsync(fixture.Tenant.Id);

        Assert.Equal($"https://portal.example/{fixture.Tenant.Id}", result.Url);
    }

    [Fact]
    public async Task Purchasable_plans_hide_inactive_and_unsellable_plans_but_keep_the_current_one()
    {
        var fixture = new Fixture();
        var current = fixture.AddPlan("legacy", price: 0, interval: BillingInterval.None, externalPriceId: null, active: false);
        fixture.AddPlan("pro");
        fixture.AddPlan("retired", active: false);
        fixture.AddPlan("manual-only", price: 0, interval: BillingInterval.None, externalPriceId: null);
        fixture.Tenant.AssignSubscriptionPlan(current.Id);

        var plans = await fixture.Service.GetPurchasablePlansAsync(fixture.Tenant.Id);

        Assert.Equal(["legacy", "pro"], plans.Select(p => p.Key).ToArray());
        Assert.True(plans.Single(p => p.Key == "legacy").IsCurrent);
        Assert.False(plans.Single(p => p.Key == "pro").IsCurrent);
    }
}
