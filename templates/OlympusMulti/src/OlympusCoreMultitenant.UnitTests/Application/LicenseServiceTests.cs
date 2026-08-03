using Microsoft.Extensions.Options;
using OlympusCoreMultitenant.Application.Common.Configuration;
using OlympusCoreMultitenant.Application.Licensing;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;
using OlympusCoreMultitenant.UnitTests.TestSupport;

namespace OlympusCoreMultitenant.UnitTests.Application;

public class LicenseServiceTests
{
    private static LicenseService CreateService(int gracePeriodDays = 7) =>
        new(new FakeTenantRepository(), Options.Create(new LicensingOptions { GracePeriodDays = gracePeriodDays }));

    [Fact]
    public void No_trial_and_no_expiry_is_Active()
    {
        var tenant = new Tenant("acme", "Acme Inc");

        var status = CreateService().ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Active, status);
    }

    [Fact]
    public void Within_trial_window_is_Trial()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.StartTrial(DateTime.UtcNow.AddDays(3));

        var status = CreateService().ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Trial, status);
    }

    [Fact]
    public void Expired_trial_with_no_license_expiry_falls_back_to_Active()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.StartTrial(DateTime.UtcNow.AddDays(-1));

        var status = CreateService().ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Active, status);
    }

    [Fact]
    public void Before_license_expiry_is_Active()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.SetLicenseExpiry(DateTime.UtcNow.AddDays(10));

        var status = CreateService().ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Active, status);
    }

    [Fact]
    public void Just_past_expiry_within_grace_period_is_GracePeriod()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.SetLicenseExpiry(DateTime.UtcNow.AddDays(-1));

        var status = CreateService(gracePeriodDays: 7).ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.GracePeriod, status);
    }

    [Fact]
    public void Past_expiry_and_grace_period_is_Expired()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.SetLicenseExpiry(DateTime.UtcNow.AddDays(-10));

        var status = CreateService(gracePeriodDays: 7).ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Expired, status);
    }

    [Fact]
    public void Suspended_overrides_an_otherwise_active_trial_and_license()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.StartTrial(DateTime.UtcNow.AddDays(10));
        tenant.SetLicenseExpiry(DateTime.UtcNow.AddDays(10));
        tenant.Suspend();

        var status = CreateService().ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Suspended, status);
    }

    [Fact]
    public void Unsuspend_reverts_to_the_status_the_underlying_dates_imply()
    {
        var tenant = new Tenant("acme", "Acme Inc");
        tenant.Suspend();
        tenant.Unsuspend();

        var status = CreateService().ComputeStatus(tenant);

        Assert.Equal(LicenseStatus.Active, status);
    }

    [Theory]
    [InlineData(LicenseStatus.Active, true)]
    [InlineData(LicenseStatus.Trial, true)]
    [InlineData(LicenseStatus.GracePeriod, true)]
    [InlineData(LicenseStatus.Expired, false)]
    [InlineData(LicenseStatus.Suspended, false)]
    public void IsAccessAllowed_matches_the_documented_status_to_access_mapping(LicenseStatus status, bool expected)
    {
        Assert.Equal(expected, CreateService().IsAccessAllowed(status));
    }
}
