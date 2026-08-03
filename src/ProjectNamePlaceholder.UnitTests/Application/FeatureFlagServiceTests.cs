using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.FeatureFlags;
using ProjectNamePlaceholder.Application.FeatureFlags.Dtos;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Application;

public class FeatureFlagServiceTests
{
    private static FeatureFlagService CreateService(out CurrentTenantService tenantService)
    {
        tenantService = new CurrentTenantService();
        return new FeatureFlagService(
            new FakeTenantFeatureFlagRepository(),
            new FakeUnitOfWork(),
            new FakeAppCache(),
            tenantService,
            Options.Create(new CachingOptions()));
    }

    [Fact]
    public async Task IsEnabledAsync_defaults_to_false_when_no_flag_has_been_set()
    {
        var service = CreateService(out var tenantService);
        using var scope = tenantService.BeginScope(1, "acme");

        var enabled = await service.IsEnabledAsync("new-checkout");

        Assert.False(enabled);
    }

    [Fact]
    public async Task SetAsync_then_IsEnabledAsync_reflects_the_new_value_immediately()
    {
        var service = CreateService(out var tenantService);
        using var scope = tenantService.BeginScope(1, "acme");

        await service.SetAsync(new SetTenantFeatureFlagRequestDto("new-checkout", IsEnabled: true, RolloutPercentage: 100));

        Assert.True(await service.IsEnabledAsync("new-checkout"));
    }

    [Fact]
    public async Task Disabling_a_flag_after_enabling_it_takes_effect_and_bypasses_the_stale_cache_entry()
    {
        var service = CreateService(out var tenantService);
        using var scope = tenantService.BeginScope(1, "acme");

        await service.SetAsync(new SetTenantFeatureFlagRequestDto("new-checkout", IsEnabled: true, RolloutPercentage: 100));
        Assert.True(await service.IsEnabledAsync("new-checkout"));

        await service.SetAsync(new SetTenantFeatureFlagRequestDto("new-checkout", IsEnabled: false, RolloutPercentage: 100));

        Assert.False(await service.IsEnabledAsync("new-checkout"));
    }

    [Fact]
    public async Task Zero_percent_rollout_is_always_disabled_even_when_IsEnabled_is_true()
    {
        var service = CreateService(out var tenantService);
        using var scope = tenantService.BeginScope(1, "acme");

        await service.SetAsync(new SetTenantFeatureFlagRequestDto("new-checkout", IsEnabled: true, RolloutPercentage: 0));

        Assert.False(await service.IsEnabledAsync("new-checkout"));
    }

    [Fact]
    public async Task Rollout_bucket_is_stable_for_the_same_tenant_and_key_across_calls()
    {
        var service = CreateService(out var tenantService);
        using var scope = tenantService.BeginScope(7, "acme");

        await service.SetAsync(new SetTenantFeatureFlagRequestDto("beta-search", IsEnabled: true, RolloutPercentage: 50));

        var first = await service.IsEnabledAsync("beta-search");
        var second = await service.IsEnabledAsync("beta-search");

        Assert.Equal(first, second);
    }
}
