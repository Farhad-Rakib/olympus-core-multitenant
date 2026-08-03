using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.UnitTests.Domain;

public class TenantTests
{
    [Fact]
    public void Disable_sets_IsActive_false_and_stamps_DisabledAtUtc()
    {
        var tenant = new Tenant("acme", "Acme Inc");

        tenant.Disable();

        Assert.False(tenant.IsActive);
        Assert.NotNull(tenant.DisabledAtUtc);
    }

    [Fact]
    public void Enable_clears_DisabledAtUtc_so_a_later_purge_check_cannot_reuse_a_stale_timestamp()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.Disable();

        tenant.Enable();

        Assert.True(tenant.IsActive);
        Assert.Null(tenant.DisabledAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_a_blank_slug(string slug)
    {
        Assert.Throws<ArgumentException>(() => new Tenant(slug, "Acme Inc"));
    }
}
