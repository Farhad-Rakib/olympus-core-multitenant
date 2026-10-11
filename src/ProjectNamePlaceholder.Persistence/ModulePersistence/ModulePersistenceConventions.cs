using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectNamePlaceholder.Domain.Common;

namespace ProjectNamePlaceholder.Persistence.ModulePersistence;

// Enforces the module persistence boundary on the built model:
//
//  1. Every [OwnedByModule("x")] entity is mapped into schema "x" (Postgres schema / SQL Server
//     schema), so a module's tables are visibly separate and can be dropped or exported as a unit.
//  2. Dependency direction. A foreign key may point from a module entity to a core entity, never
//     from core to a module, and never between two modules. Violations throw at model build --
//     i.e. at first startup or in the unit tests -- rather than being discovered when someone
//     tries to remove a module and finds core tables referencing it.
//
// This is the boundary the physical project split (README roadmap #2) will formalise; until then
// the shared DbContext plus these rules give the same guarantees at model-build time.
public static class ModulePersistenceConventions
{
    public static string? ModuleKeyOf(Type clrType) =>
        clrType.GetCustomAttributes(typeof(OwnedByModuleAttribute), inherit: false)
            .OfType<OwnedByModuleAttribute>()
            .FirstOrDefault()?.ModuleKey;

    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }

            var moduleKey = ModuleKeyOf(entityType.ClrType);
            if (moduleKey is null)
            {
                continue;
            }

            // A table name is required for schema to apply; configurations normally set one, but a
            // module entity registered by convention alone still needs a deterministic name.
            var tableName = entityType.GetTableName() ?? entityType.GetDefaultTableName() ?? entityType.ClrType.Name;
            entityType.SetTableName(tableName);
            entityType.SetSchema(moduleKey);
        }

        ValidateDependencyDirection(modelBuilder.Model);
    }

    public static void ValidateDependencyDirection(IMutableModel model)
    {
        var violations = new List<string>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var dependentModule = ModuleKeyOf(entityType.ClrType);

            foreach (var fk in entityType.GetForeignKeys())
            {
                var principalModule = ModuleKeyOf(fk.PrincipalEntityType.ClrType);

                // core -> core, module -> core, module -> same module: all fine.
                if (principalModule is null || principalModule == dependentModule)
                {
                    continue;
                }

                var what = dependentModule is null ? "core entity" : $"module '{dependentModule}' entity";
                violations.Add($"{what} {entityType.ClrType.Name} references module '{principalModule}' entity {fk.PrincipalEntityType.ClrType.Name}");
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Module persistence boundary violated. Core must not reference module entities and modules must not reference each other:\n - "
                + string.Join("\n - ", violations));
        }
    }
}
