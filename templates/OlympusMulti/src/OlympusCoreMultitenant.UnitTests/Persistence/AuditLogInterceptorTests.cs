using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;
using OlympusCoreMultitenant.Infrastructure.MultiTenancy;
using OlympusCoreMultitenant.UnitTests.TestSupport;

namespace OlympusCoreMultitenant.UnitTests.Persistence;

// Covers ApplicationDbContext's generic SaveChanges audit interceptor -- the other invariant the
// architecture review flagged as needing regression coverage before it's safe to keep extending.
public class AuditLogInterceptorTests
{
    [Fact]
    public async Task Creating_an_entity_records_a_Created_row_with_tenant_and_actor()
    {
        var tenantService = new CurrentTenantService();
        var userService = new CurrentUserService();
        await using var dbContext = TestDbContextFactory.Create(tenantService, userService);

        var tenant = new Tenant("tenant-a", "Tenant A");
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        userService.Set(42);
        using (tenantService.BeginScope(tenant.Id, tenant.Slug))
        {
            dbContext.SiteSettings.Add(new SiteSetting { TenantId = tenant.Id, Key = "theme", Value = "dark" });
            await dbContext.SaveChangesAsync();
        }

        var audit = await dbContext.AuditLogs.SingleAsync(a => a.EntityName == nameof(SiteSetting));
        Assert.Equal(AuditAction.Created, audit.Action);
        Assert.Equal(tenant.Id, audit.TenantId);
        Assert.Equal(42, audit.UserId);
        Assert.Contains("dark", audit.Changes);
    }

    [Fact]
    public async Task Updating_an_entity_only_captures_properties_that_actually_changed()
    {
        var tenantService = new CurrentTenantService();
        await using var dbContext = TestDbContextFactory.Create(tenantService);

        var tenant = new Tenant("tenant-a", "Tenant A");
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        SiteSetting setting;
        using (tenantService.BeginScope(tenant.Id, tenant.Slug))
        {
            setting = new SiteSetting { TenantId = tenant.Id, Key = "theme", Value = "dark", Description = "UI theme" };
            dbContext.SiteSettings.Add(setting);
            await dbContext.SaveChangesAsync();

            setting.Value = "light";
            await dbContext.SaveChangesAsync();
        }

        var audit = await dbContext.AuditLogs.SingleAsync(a => a.EntityName == nameof(SiteSetting) && a.Action == AuditAction.Updated);
        Assert.Contains("light", audit.Changes);
        Assert.DoesNotContain("UI theme", audit.Changes);
    }

    [Fact]
    public async Task RefreshTokens_are_excluded_from_the_audit_trail()
    {
        var tenantService = new CurrentTenantService();
        await using var dbContext = TestDbContextFactory.Create(tenantService);

        var tenant = new Tenant("tenant-a", "Tenant A");
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        using (tenantService.BeginScope(tenant.Id, tenant.Slug))
        {
            var user = new User("Alice", "alice@a.com", "hash") { TenantId = tenant.Id };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            dbContext.RefreshTokens.Add(new RefreshToken(user.Id, "some-token-hash", DateTime.UtcNow.AddDays(1)) { TenantId = tenant.Id });
            await dbContext.SaveChangesAsync();
        }

        var refreshTokenAudits = await dbContext.AuditLogs.Where(a => a.EntityName == nameof(RefreshToken)).ToListAsync();
        Assert.Empty(refreshTokenAudits);
    }
}
