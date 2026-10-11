using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.Persistence.Context;
using ProjectNamePlaceholder.Persistence.Offboarding;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Persistence;

// Purging a tenant used to leave its audit rows behind as orphans still holding user names and
// email addresses (CaptureAuditEntries records every non-secret property). These pin whichever
// handling is configured actually happens.
public class TenantPurgeAuditHandlingTests
{
    private static async Task<(ApplicationDbContext Db, Tenant Tenant, TenantOffboardingService Service, FakeFileStorage Files)> SetupAsync(
        PurgedTenantAuditHandling handling,
        PurgedTenantFileHandling fileHandling = PurgedTenantFileHandling.Delete)
    {
        var tenantService = new CurrentTenantService();
        var dbContext = TestDbContextFactory.Create(tenantService);

        var tenant = new Tenant("acme", "Acme Corp");
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.Id,
            UserId = 42,
            EntityName = "User",
            Action = AuditAction.Created,
            Changes = "{\"Email\":\"person@acme.com\",\"FullName\":\"A Person\"}",
            CreatedAt = DateTime.UtcNow
        });

        // A second tenant's row, which must never be touched by another tenant's purge.
        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.Id + 999,
            UserId = 7,
            EntityName = "User",
            Action = AuditAction.Created,
            Changes = "{\"Email\":\"other@example.com\"}",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();

        var service = new TenantOffboardingService(
            dbContext,
            tenantService,
            fileStorage,
            Options.Create(new TenantOffboardingOptions
            {
                AuditHandling = handling,
                FileHandling = fileHandling
            }));

        return (dbContext, tenant, service, fileStorage);
    }

    [Fact]
    public async Task Anonymize_keeps_the_row_but_strips_the_personal_data()
    {
        var (db, tenant, service, _) = await SetupAsync(PurgedTenantAuditHandling.Anonymize);

        await service.PurgeAsync(tenant);

        var log = db.AuditLogs.Single(l => l.TenantId == tenant.Id);
        Assert.Null(log.UserId);
        Assert.DoesNotContain("person@acme.com", log.Changes);
        Assert.DoesNotContain("A Person", log.Changes);

        // The shape of the history survives: what happened, when, and to which entity type.
        Assert.Equal("User", log.EntityName);
        Assert.Equal(AuditAction.Created, log.Action);
    }

    [Fact]
    public async Task Delete_removes_the_purged_tenants_rows_entirely()
    {
        var (db, tenant, service, _) = await SetupAsync(PurgedTenantAuditHandling.Delete);

        await service.PurgeAsync(tenant);

        Assert.Empty(db.AuditLogs.Where(l => l.TenantId == tenant.Id));
    }

    [Fact]
    public async Task Retain_leaves_the_trail_untouched()
    {
        var (db, tenant, service, _) = await SetupAsync(PurgedTenantAuditHandling.Retain);

        await service.PurgeAsync(tenant);

        var log = db.AuditLogs.Single(l => l.TenantId == tenant.Id);
        Assert.Equal(42, log.UserId);
        Assert.Contains("person@acme.com", log.Changes);
    }

    [Fact]
    public async Task Purging_deletes_the_tenants_uploaded_files_by_default()
    {
        var (_, tenant, service, files) = await SetupAsync(PurgedTenantAuditHandling.Anonymize);

        await service.PurgeAsync(tenant);

        Assert.Equal([tenant.Id], files.DeletedTenantIds);
    }

    [Fact]
    public async Task Retain_leaves_the_tenants_uploaded_files_on_disk()
    {
        var (_, tenant, service, files) = await SetupAsync(
            PurgedTenantAuditHandling.Anonymize,
            PurgedTenantFileHandling.Retain);

        await service.PurgeAsync(tenant);

        Assert.Empty(files.DeletedTenantIds);
    }

    [Fact]
    public async Task Another_tenants_audit_rows_are_never_affected()
    {
        var (db, tenant, service, _) = await SetupAsync(PurgedTenantAuditHandling.Delete);
        var otherTenantId = tenant.Id + 999;

        await service.PurgeAsync(tenant);

        var untouched = db.AuditLogs.Single(l => l.TenantId == otherTenantId);
        Assert.Equal(7, untouched.UserId);
        Assert.Contains("other@example.com", untouched.Changes);
    }
}
