using ProjectNamePlaceholder.Application.AuditLogs;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.Persistence.Repositories;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Persistence;

// AuditLog is deliberately NOT an ITenantEntity, so it has no global query filter -- these tests
// pin the manual scoping in AuditLogRepository that stands in for one.
public class AuditLogScopingTests
{
    private static async Task<AuditLogRepository> SeedTwoTenantsAsync()
    {
        var dbContext = TestDbContextFactory.Create(new CurrentTenantService());

        dbContext.AuditLogs.AddRange(
            new AuditLog { TenantId = 1, EntityName = "User", Action = AuditAction.Created },
            new AuditLog { TenantId = 1, EntityName = "Role", Action = AuditAction.Updated },
            new AuditLog { TenantId = 2, EntityName = "User", Action = AuditAction.Created },
            new AuditLog { TenantId = null, EntityName = "Tenant", Action = AuditAction.Created });

        await dbContext.SaveChangesAsync();
        return new AuditLogRepository(dbContext);
    }

    [Fact]
    public async Task EnforcedTenantId_limits_results_to_that_tenant()
    {
        var repository = await SeedTwoTenantsAsync();

        var (items, total) = await repository.GetPagedAsync(new AuditLogQuery { EnforcedTenantId = 1 });

        Assert.Equal(2, total);
        Assert.All(items, log => Assert.Equal(1, log.TenantId));
    }

    [Fact]
    public async Task EnforcedTenantId_overrides_a_conflicting_caller_supplied_TenantId()
    {
        var repository = await SeedTwoTenantsAsync();

        // The hostile case: a tenant-1 caller hand-crafting ?tenantId=2 to read another tenant.
        var (items, total) = await repository.GetPagedAsync(new AuditLogQuery
        {
            EnforcedTenantId = 1,
            TenantId = 2
        });

        Assert.Equal(2, total);
        Assert.All(items, log => Assert.Equal(1, log.TenantId));
    }

    [Fact]
    public async Task EnforcedTenantId_hides_platform_global_rows_that_belong_to_no_tenant()
    {
        var repository = await SeedTwoTenantsAsync();

        var (items, _) = await repository.GetPagedAsync(new AuditLogQuery { EnforcedTenantId = 1 });

        Assert.DoesNotContain(items, log => log.TenantId is null);
    }

    [Fact]
    public async Task No_enforced_tenant_keeps_the_cross_tenant_view_for_platform_admins()
    {
        var repository = await SeedTwoTenantsAsync();

        var (_, total) = await repository.GetPagedAsync(new AuditLogQuery());

        Assert.Equal(4, total);
    }
}
