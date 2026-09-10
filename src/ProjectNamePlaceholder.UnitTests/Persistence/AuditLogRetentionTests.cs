using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.Persistence.Context;
using ProjectNamePlaceholder.Persistence.Maintenance;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Persistence;

public class AuditLogRetentionTests
{
    private static async Task<ApplicationDbContext> SeedAsync(params int[] ageInDays)
    {
        var dbContext = TestDbContextFactory.Create(new CurrentTenantService());

        foreach (var age in ageInDays)
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                TenantId = 1,
                EntityName = "User",
                Action = AuditAction.Created,
                CreatedAt = DateTime.UtcNow.AddDays(-age)
            });
        }

        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private static AuditLogRetentionService Create(ApplicationDbContext dbContext, AuditLogOptions options)
        => new(dbContext, Options.Create(options));

    [Fact]
    public async Task Retention_disabled_by_default_deletes_nothing()
    {
        var dbContext = await SeedAsync(500, 900);
        var service = Create(dbContext, new AuditLogOptions()); // RetentionDays defaults to 0

        var deleted = await service.PurgeExpiredAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(2, dbContext.AuditLogs.Count());
    }

    [Fact]
    public async Task Only_rows_older_than_the_cutoff_are_removed()
    {
        var dbContext = await SeedAsync(10, 40, 100);
        var service = Create(dbContext, new AuditLogOptions { RetentionDays = 30 });

        await service.PurgeExpiredAsync();

        var remaining = dbContext.AuditLogs.ToList();
        Assert.Single(remaining);
        Assert.True(remaining[0].CreatedAt > DateTime.UtcNow.AddDays(-30));
    }

    [Fact]
    public async Task A_run_stops_at_the_batch_ceiling_and_leaves_the_rest_for_the_next_sweep()
    {
        var ages = Enumerable.Repeat(100, 10).ToArray();
        var dbContext = await SeedAsync(ages);

        // 2 batches x 3 rows = 6 of the 10 expired rows in one run.
        var service = Create(dbContext, new AuditLogOptions
        {
            RetentionDays = 30,
            CleanupBatchSize = 3,
            MaxBatchesPerRun = 2
        });

        var deleted = await service.PurgeExpiredAsync();

        Assert.Equal(6, deleted);
        Assert.Equal(4, dbContext.AuditLogs.Count());
    }
}
