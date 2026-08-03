# SampleModule

Scaffolded by `dotnet new olympus-module -n SampleModule`.

## What just happened

`SampleModuleModule.cs` implements `IOlympusModule`. That's the entire registration step —
`ModuleHost.DiscoverModules` (called once at boot in `Program.cs`) assembly-scans for
`IOlympusModule` implementations, so this module is picked up automatically the next time the app
starts. Nothing else needs editing: not `Program.cs`, not `Permissions.cs`, not `Modules.cs`, not
`DefaultMenuSeeder`.

## Next steps

1. Rename the placeholder permission keys (`samplemodule.read`, etc.) and menu entry to match what
   this module actually does.
2. Add entities to `ProjectNamePlaceholder.Domain.Entities` (or wherever your project's entities
   live) implementing `ITenantEntity` if they're per-tenant data.
3. Add a repository interface + EF configuration + repository implementation in the Persistence
   project, following an existing module's shape (e.g. `TenantFeatureFlagRepository` is a small,
   representative example).
4. Add a service (`ISampleModuleService`/`SampleModuleService`) and register it in
   `Application/DependencyInjection.cs`.
5. Add a controller gated by this module's permissions
   (`[Authorize(Policy = "samplemodule.read")]`).
6. Run `dotnet new olympus-feature --module SampleModule --feature YourFeatureName` to scaffold a
   single vertical-slice feature (command/validator/handler/endpoint) inside this module.

Delete this README once the module is real.
