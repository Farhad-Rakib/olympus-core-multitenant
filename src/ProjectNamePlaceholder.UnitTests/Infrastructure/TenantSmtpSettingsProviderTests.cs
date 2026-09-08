using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Infrastructure.Email;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Infrastructure;

public class TenantSmtpSettingsProviderTests
{
    private static readonly SmtpOptions PlatformDefaults = new()
    {
        Host = "smtp.platform.local",
        Port = 587,
        Username = "platform-user",
        Password = "platform-password",
        FromAddress = "no-reply@platform.local",
        FromName = "Platform",
        UseSsl = true
    };

    private static TenantSmtpSettingsProvider CreateProvider(params (string Key, string Value)[] tenantSettings) =>
        new(new FakeSiteSettingRepository(tenantSettings), Options.Create(PlatformDefaults));

    [Fact]
    public async Task Falls_back_to_platform_defaults_when_the_tenant_has_overridden_nothing()
    {
        var provider = CreateProvider();

        var settings = await provider.ResolveAsync();

        Assert.Equal("smtp.platform.local", settings.Host);
        Assert.Equal(587, settings.Port);
        Assert.Equal("no-reply@platform.local", settings.FromAddress);
        Assert.Equal("Platform", settings.FromName);
        Assert.True(settings.UseSsl);
    }

    [Fact]
    public async Task Tenant_site_settings_override_the_platform_defaults()
    {
        var provider = CreateProvider(
            ("Smtp.Host", "smtp.acme.com"),
            ("Smtp.Port", "2525"),
            ("Smtp.Username", "acme-user"),
            ("Smtp.Password", "acme-password"),
            ("Smtp.FromEmail", "billing@acme.com"),
            ("Smtp.FromName", "Acme Billing"),
            ("Smtp.UseSsl", "false"));

        var settings = await provider.ResolveAsync();

        Assert.Equal("smtp.acme.com", settings.Host);
        Assert.Equal(2525, settings.Port);
        Assert.Equal("acme-user", settings.Username);
        Assert.Equal("acme-password", settings.Password);
        Assert.Equal("billing@acme.com", settings.FromAddress);
        Assert.Equal("Acme Billing", settings.FromName);
        Assert.False(settings.UseSsl);
    }

    [Fact]
    public async Task Overrides_are_resolved_per_key_not_all_or_nothing()
    {
        var provider = CreateProvider(("Smtp.FromName", "Acme Support"));

        var settings = await provider.ResolveAsync();

        Assert.Equal("Acme Support", settings.FromName);
        Assert.Equal("smtp.platform.local", settings.Host);
        Assert.Equal("no-reply@platform.local", settings.FromAddress);
    }

    [Fact]
    public async Task Blank_tenant_values_fall_back_instead_of_sending_from_an_empty_address()
    {
        var provider = CreateProvider(
            ("Smtp.FromEmail", "   "),
            ("Smtp.Host", ""));

        var settings = await provider.ResolveAsync();

        Assert.Equal("no-reply@platform.local", settings.FromAddress);
        Assert.Equal("smtp.platform.local", settings.Host);
    }

    [Fact]
    public async Task Unparseable_port_falls_back_rather_than_failing_the_send()
    {
        var provider = CreateProvider(("Smtp.Port", "not-a-number"));

        var settings = await provider.ResolveAsync();

        Assert.Equal(587, settings.Port);
    }
}
