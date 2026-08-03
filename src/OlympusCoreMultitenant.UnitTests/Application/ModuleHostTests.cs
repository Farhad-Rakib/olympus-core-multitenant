using OlympusCoreMultitenant.Application.ModuleSystem;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.UnitTests.Application;

public class ModuleHostTests
{
    [Fact]
    public void DiscoverModules_finds_Core_and_Platform_from_the_Application_assembly()
    {
        var modules = new ModuleHost().DiscoverModules([typeof(Permissions).Assembly]);

        Assert.Contains(modules, m => m.Key == Modules.Core);
        Assert.Contains(modules, m => m.Key == Modules.Platform);
    }

    [Fact]
    public void No_permission_is_declared_by_more_than_one_discovered_module()
    {
        var modules = new ModuleHost().DiscoverModules([typeof(Permissions).Assembly]);

        var names = modules.SelectMany(m => m.Permissions).Select(p => p.Name).ToList();

        Assert.Equal(names.Distinct(StringComparer.OrdinalIgnoreCase).Count(), names.Count);
    }

    [Fact]
    public void Every_Permissions_constant_is_owned_by_exactly_one_discovered_module()
    {
        var modules = new ModuleHost().DiscoverModules([typeof(Permissions).Assembly]);
        var declared = modules.SelectMany(m => m.Permissions).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in Permissions.All)
        {
            Assert.Contains(permission, declared);
        }
    }

    // A fake module used only to exercise ModuleHost's dependency validation -- a real
    // IOlympusModule type in this same test assembly (with a parameterless constructor, since
    // DiscoverModules activates every implementation it finds via Activator.CreateInstance), so
    // DiscoverModules picks it up via the same reflection path it uses in production.
    private sealed class FakeModuleWithMissingDependency : IOlympusModule
    {
        public string Key => "test.needs-missing";
        public string Name => "Needs Missing";
        public string Description => "";
        public ModuleKind Kind => ModuleKind.Business;
        public IReadOnlyList<string> Dependencies => ["test.does-not-exist"];
        public IReadOnlyList<string> OptionalDependencies => [];
        public IReadOnlyList<PermissionDefinition> Permissions => [];
        public IReadOnlyList<MenuDefinition> Menus => [];
    }

    [Fact]
    public void DiscoverModules_throws_when_a_required_dependency_is_not_discovered()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ModuleHost().DiscoverModules([typeof(FakeModuleWithMissingDependency).Assembly]));

        Assert.Contains("test.needs-missing", ex.Message);
        Assert.Contains("test.does-not-exist", ex.Message);
    }
}
