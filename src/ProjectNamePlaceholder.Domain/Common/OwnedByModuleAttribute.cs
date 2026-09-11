namespace ProjectNamePlaceholder.Domain.Common;

// Marks an entity as belonging to a business module (the IOlympusModule with this key). The
// persistence layer uses it to put the module's tables in their own database schema and to
// enforce the dependency direction: modules may reference core entities (Tenant, User...), core
// may never reference a module's entities, and modules may not reference each other. Unmarked
// entities are core/platform and live in the default schema, exactly as before.
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OwnedByModuleAttribute : Attribute
{
    public OwnedByModuleAttribute(string moduleKey)
    {
        ModuleKey = !string.IsNullOrWhiteSpace(moduleKey)
            ? moduleKey.Trim().ToLowerInvariant()
            : throw new ArgumentException("Module key is required.", nameof(moduleKey));
    }

    public string ModuleKey { get; }
}
