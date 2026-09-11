using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.ModulePersistence;

// EF caches the built model per (context type, design-time) by default. Two ApplicationDbContext
// instances with different module assembly sets would otherwise share one cached model and the
// second set's entities would silently be missing. The key includes the assembly set so each
// combination builds -- and caches -- its own model. (A schema-per-tenant tier would extend this
// same key with the tenant schema; this is the hook it needs.)
public sealed class ModuleAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        var signature = context is ApplicationDbContext app ? app.ModulePersistenceSignature : string.Empty;
        return (context.GetType(), designTime, signature);
    }
}
