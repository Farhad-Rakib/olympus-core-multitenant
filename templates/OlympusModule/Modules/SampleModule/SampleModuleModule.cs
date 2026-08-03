using ProjectNamePlaceholder.Application.ModuleSystem;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.BusinessModules.SampleModule;

// Namespace is BusinessModules, not Modules, deliberately: ProjectNamePlaceholder.Application
// .Security.Modules is a pre-existing static class, and C#'s enclosing-namespace lookup makes any
// namespace literally named "Modules" ambiguous with it from files that also
// `using ProjectNamePlaceholder.Application.Security;`. The folder is still called Modules/ to
// match the framework's own vocabulary -- only the C# namespace differs.
//
// Scaffolded by `dotnet new olympus-module`. Implementing IOlympusModule is the only step
// required for ModuleHost to discover this module at boot -- no manual registration in
// Program.cs, no edits to Permissions.cs/Modules.cs. See CoreModule/PlatformModule
// (Application/ModuleSystem/) for the reference implementations this mirrors.
//
// Dependencies lists other module Keys this one requires to already be discovered ("core" is
// almost always required since it owns users/roles/permissions). Add real permissions/menus as
// the module grows -- these four are placeholders.
public sealed class SampleModuleModule : IOlympusModule
{
    public string Key => "samplemodule";
    public string Name => "SampleModule";
    public string Description => "TODO: describe what this module owns.";
    public ModuleKind Kind => ModuleKind.Business;
    public IReadOnlyList<string> Dependencies => ["core"];
    public IReadOnlyList<string> OptionalDependencies => [];

    public IReadOnlyList<PermissionDefinition> Permissions =>
    [
        new("samplemodule.read", "View SampleModule records"),
        new("samplemodule.create", "Create SampleModule records"),
        new("samplemodule.update", "Update SampleModule records"),
        new("samplemodule.delete", "Delete SampleModule records"),
    ];

    public IReadOnlyList<MenuDefinition> Menus =>
    [
        new("SampleModule", "/samplemodule", "package", "samplemodule.read", "Configuration"),
    ];
}
