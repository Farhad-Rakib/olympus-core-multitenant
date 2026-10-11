# Module persistence boundary

Every module's tables still live in the one `ApplicationDbContext`, but the boundary between core
and modules is now enforced at model-build time rather than by convention.

## Declaring a module's entities

```csharp
// Domain -- no EF here
[OwnedByModule("billing")]
public sealed class Invoice : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }          // tenant filter is automatic, as always
    public decimal Amount { get; set; }
}
```

```csharp
// The module's assembly (may be a separate project referencing Persistence)
public sealed class BillingModelContributor : IModuleModelContributor
{
    public string ModuleKey => "billing";
    public void Configure(ModelBuilder b) => b.Entity<Invoice>().ToTable("invoices");
}
```

```csharp
// Program.cs
builder.Services.AddPersistence(builder.Configuration, modules =>
    modules.ModuleAssemblies.Add(typeof(BillingModelContributor).Assembly));
```

`IEntityTypeConfiguration<T>` classes in a registered assembly are applied too.

## What the conventions enforce (`ModulePersistenceConventions`)

1. **Schema per module.** `[OwnedByModule("billing")]` entities are mapped to schema `billing`
   (`billing.invoices`). Core/platform entities stay in the default schema. A module's data is
   therefore droppable/exportable as a unit.
2. **Dependency direction.** Foreign keys may point module → core and module → same module.
   A core → module or module → module FK **throws at startup** (and in the unit tests) naming the
   offending entities. This is what keeps a module removable.
3. **Model cache key** includes the registered assembly set (`ModuleAwareModelCacheKeyFactory`),
   so two contexts with different module sets never share a cached model.

## Migrations

Migrations for module tables go through the same script (`scripts/ef-migration.sh`), because the
snapshot is one model. If a module ships as a separate assembly it must be registered in
`ApplicationDbContextFactory` (design time) as well as in `AddPersistence` (runtime).

## What this is not

It is not the physical project split (README roadmap #2). That split would move each module's
Application/Persistence code into `Modules/<Key>/*` projects; the seam above is what those
projects would plug into, so doing the split later is a file move, not a redesign.
