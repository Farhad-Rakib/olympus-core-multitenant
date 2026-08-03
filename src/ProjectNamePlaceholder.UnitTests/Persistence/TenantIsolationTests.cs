using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Infrastructure.MultiTenancy;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Persistence;

// Regression coverage for the single most load-bearing invariant in the codebase: the global
// query filter in ApplicationDbContext that keeps one tenant's rows from ever being visible to
// another. See CLAUDE.md / architecture review for why this has historically had zero test coverage.
public class TenantIsolationTests
{
    [Fact]
    public async Task Users_query_only_returns_rows_for_the_active_tenant_scope()
    {
        var tenantService = new CurrentTenantService();
        await using var dbContext = TestDbContextFactory.Create(tenantService);

        var tenantA = new Tenant("tenant-a", "Tenant A");
        var tenantB = new Tenant("tenant-b", "Tenant B");
        dbContext.Tenants.AddRange(tenantA, tenantB);
        await dbContext.SaveChangesAsync();

        using (tenantService.BeginScope(tenantA.Id, tenantA.Slug))
        {
            dbContext.Users.Add(new User("Alice", "alice@a.com", "hash"));
            await dbContext.SaveChangesAsync();
        }

        using (tenantService.BeginScope(tenantB.Id, tenantB.Slug))
        {
            dbContext.Users.Add(new User("Bob", "bob@b.com", "hash"));
            await dbContext.SaveChangesAsync();
        }

        using (tenantService.BeginScope(tenantA.Id, tenantA.Slug))
        {
            var users = await dbContext.Users.ToListAsync();
            var user = Assert.Single(users);
            Assert.Equal("Alice", user.FullName);
        }

        using (tenantService.BeginScope(tenantB.Id, tenantB.Slug))
        {
            var users = await dbContext.Users.ToListAsync();
            var user = Assert.Single(users);
            Assert.Equal("Bob", user.FullName);
        }
    }

    [Fact]
    public async Task Query_without_any_tenant_scope_returns_no_rows_from_any_tenant()
    {
        var tenantService = new CurrentTenantService();
        await using var dbContext = TestDbContextFactory.Create(tenantService);

        var tenant = new Tenant("tenant-a", "Tenant A");
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        using (tenantService.BeginScope(tenant.Id, tenant.Slug))
        {
            dbContext.Users.Add(new User("Alice", "alice@a.com", "hash"));
            await dbContext.SaveChangesAsync();
        }

        // No BeginScope active here: CurrentTenantId is null, and the filter is
        // "entity.TenantId == CurrentTenantId" -- this must fail closed (empty), never leak rows
        // from every tenant just because no tenant was specified.
        var users = await dbContext.Users.ToListAsync();
        Assert.Empty(users);
    }

    [Fact]
    public async Task BeginScope_restores_the_previous_tenant_on_dispose()
    {
        var tenantService = new CurrentTenantService();
        await using var dbContext = TestDbContextFactory.Create(tenantService);

        var tenantA = new Tenant("tenant-a", "Tenant A");
        dbContext.Tenants.Add(tenantA);
        await dbContext.SaveChangesAsync();

        using (tenantService.BeginScope(tenantA.Id, tenantA.Slug))
        {
            Assert.Equal(tenantA.Id, tenantService.TenantId);

            using (tenantService.BeginScope(999, "temp-override"))
            {
                Assert.Equal(999, tenantService.TenantId);
            }

            // Nested scope's Dispose must restore the outer tenant, not clear it entirely -- this
            // is exactly the shape TenantResolutionMiddleware's platform-admin override relies on.
            Assert.Equal(tenantA.Id, tenantService.TenantId);
        }

        Assert.Null(tenantService.TenantId);
    }
}
