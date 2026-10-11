using System.Reflection;

namespace ProjectNamePlaceholder.Persistence.ModulePersistence;

// Assemblies scanned for IModuleModelContributor implementations and IEntityTypeConfiguration<T>
// classes. The Persistence assembly itself is always included. Add a module's assembly here (via
// AddPersistence's configure callback) and its tables join the shared context under the module's
// own schema.
public sealed class ModulePersistenceOptions
{
    public List<Assembly> ModuleAssemblies { get; } = new();
}
