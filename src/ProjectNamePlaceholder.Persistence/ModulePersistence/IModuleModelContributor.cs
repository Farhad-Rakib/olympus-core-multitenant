using Microsoft.EntityFrameworkCore;

namespace ProjectNamePlaceholder.Persistence.ModulePersistence;

// The seam a business module uses to own its persistence without editing ApplicationDbContext:
// implement this in the module's assembly (which may be a separate project), register that
// assembly through ModulePersistenceOptions, and ApplicationDbContext will call Configure while
// building the model. Entities the module adds should carry [OwnedByModule(ModuleKey)] so the
// schema and dependency rules in ModulePersistenceConventions apply to them.
public interface IModuleModelContributor
{
    string ModuleKey { get; }

    void Configure(ModelBuilder modelBuilder);
}
