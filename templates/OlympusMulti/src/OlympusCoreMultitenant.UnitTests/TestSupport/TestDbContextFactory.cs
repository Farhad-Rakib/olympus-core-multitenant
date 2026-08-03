using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.UnitTests.TestSupport;

public static class TestDbContextFactory
{
    // A fresh, uniquely-named InMemory database per call -- InMemory databases with the same name
    // share state across DbContext instances, which would leak rows between otherwise-unrelated tests.
    public static ApplicationDbContext Create(ICurrentTenantService tenantService, ICurrentUserService? userService = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantService, userService ?? new OlympusCoreMultitenant.Infrastructure.MultiTenancy.CurrentUserService());
    }
}
