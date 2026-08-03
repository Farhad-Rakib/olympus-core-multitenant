using NetArchTest.Rules;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.UnitTests.Architecture;

// Encodes the dependency-direction rules the architecture review found holding only by
// convention (Persistence had accumulated one concrete crack -- see the fix in
// ApplicationDbContextFactory). These tests fail the build the moment that convention is broken
// again, instead of relying on someone noticing in review.
public class LayeringTests
{
    private const string DomainAssembly = "OlympusCoreMultitenant.Domain";
    private const string ApplicationAssembly = "OlympusCoreMultitenant.Application";
    private const string InfrastructureAssembly = "OlympusCoreMultitenant.Infrastructure";
    private const string PersistenceAssembly = "OlympusCoreMultitenant.Persistence";

    [Fact]
    public void Domain_has_no_dependency_on_any_other_layer()
    {
        var result = Types.InAssembly(typeof(Tenant).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationAssembly, InfrastructureAssembly, PersistenceAssembly, "OlympusCoreMultitenant.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_Persistence_or_Api()
    {
        var result = Types.InAssembly(typeof(OlympusCoreMultitenant.Application.Security.Permissions).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureAssembly, PersistenceAssembly, "OlympusCoreMultitenant.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Persistence_does_not_depend_on_Infrastructure()
    {
        // The one violation this repo actually had: ApplicationDbContextFactory (EF design-time
        // tooling) used to reach into Infrastructure.MultiTenancy for concrete CurrentTenantService/
        // CurrentUserService instances. Fixed by giving it local no-op stubs instead.
        var result = Types.InAssembly(typeof(OlympusCoreMultitenant.Persistence.Context.ApplicationDbContext).Assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Types violating the rule: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
