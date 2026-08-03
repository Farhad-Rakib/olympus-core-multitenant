using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.ModuleSystem;

// A module declares everything about itself once -- its catalog identity, the permissions and
// menus it owns -- instead of that information being hand-maintained in three separate places
// (Modules.All, Permissions.All + Modules.PermissionModuleMap, DefaultMenuSeeder). ModuleHost
// discovers implementations of this interface and the boot-time sync services
// (ModuleSyncService/PermissionSyncService/DefaultMenuSeeder) consume the aggregate instead of a
// static array. See TASKS.md "Module Framework" for the follow-ups this deliberately does not
// attempt yet (a real IModuleRegistry, plugin assembly loading, an Integration Hub).
public interface IOlympusModule
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    ModuleKind Kind { get; }

    // Hard requirement -- ModuleHost.DiscoverModules throws if a required dependency isn't also
    // discovered. Empty for both of today's modules; exists for when a real business module
    // (e.g. Commerce's Inventory) needs to declare "requires core".
    IReadOnlyList<string> Dependencies { get; }
    IReadOnlyList<string> OptionalDependencies { get; }

    IReadOnlyList<PermissionDefinition> Permissions { get; }
    IReadOnlyList<MenuDefinition> Menus { get; }
}

public sealed record PermissionDefinition(string Name, string Description);

// ParentTitle is resolved by title against the full aggregate menu set across every discovered
// module (not just this module's own Menus), so one module's items can parent under another
// module's menu (e.g. Platform's "Tenants" parenting under Core's "Configuration").
public sealed record MenuDefinition(string Title, string? Url, string? Icon, string? RequiredPermission, string? ParentTitle);
