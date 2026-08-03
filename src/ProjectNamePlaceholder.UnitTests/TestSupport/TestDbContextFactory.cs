using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.UnitTests.TestSupport;

public static class TestDbContextFactory
{
    // A fresh, uniquely-named InMemory database per call -- InMemory databases with the same name
    // share state across DbContext instances, which would leak rows between otherwise-unrelated tests.
    public static ApplicationDbContext Create(ICurrentTenantService tenantService, ICurrentUserService? userService = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantService, userService ?? new ProjectNamePlaceholder.Infrastructure.MultiTenancy.CurrentUserService());
    }
}
