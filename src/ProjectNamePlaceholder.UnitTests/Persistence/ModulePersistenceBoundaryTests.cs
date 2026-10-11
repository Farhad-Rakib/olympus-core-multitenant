using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Common;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;
using ProjectNamePlaceholder.Persistence.ModulePersistence;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Persistence;

public class ModulePersistenceBoundaryTests
{
    // --- A pretend "billing" module, declared the way a real one would be. ---

    [OwnedByModule("billing")]
    private sealed class Invoice : BaseEntity, ITenantEntity
    {
        public long TenantId { get; set; }
        public decimal Amount { get; set; }
    }

    [OwnedByModule("billing")]
    private sealed class InvoiceLine : BaseEntity
    {
        public long InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
    }

    [OwnedByModule("crm")]
    private sealed class Contact : BaseEntity
    {
        // crm -> billing: forbidden
        public long InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
    }

    private sealed class CoreThing : BaseEntity
    {
        // core -> billing: forbidden
        public long InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
    }

    private static ModelBuilder Build(Action<ModelBuilder> configure)
    {
        var builder = new ModelBuilder();
        builder.Entity<Tenant>();
        configure(builder);
        return builder;
    }

    [Fact]
    public void Module_entities_land_in_the_module_schema_core_stays_default()
    {
        var builder = Build(b =>
        {
            b.Entity<Invoice>().ToTable("invoices");
            b.Entity<InvoiceLine>();
        });

        ModulePersistenceConventions.Apply(builder);

        var invoice = builder.Model.FindEntityType(typeof(Invoice))!;
        Assert.Equal("billing", invoice.GetSchema());
        Assert.Equal("invoices", invoice.GetTableName());
        Assert.Equal("billing", builder.Model.FindEntityType(typeof(InvoiceLine))!.GetSchema());
        Assert.Null(builder.Model.FindEntityType(typeof(Tenant))!.GetSchema());
    }

    [Fact]
    public void Module_may_reference_core_and_itself()
    {
        var builder = Build(b =>
        {
            b.Entity<Invoice>().HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId);
            b.Entity<InvoiceLine>().HasOne(l => l.Invoice).WithMany().HasForeignKey(l => l.InvoiceId);
        });

        ModulePersistenceConventions.Apply(builder); // no throw
    }

    [Fact]
    public void Core_must_not_reference_a_module()
    {
        var builder = Build(b =>
        {
            b.Entity<Invoice>();
            b.Entity<CoreThing>().HasOne(c => c.Invoice).WithMany().HasForeignKey(c => c.InvoiceId);
        });

        var ex = Assert.Throws<InvalidOperationException>(() => ModulePersistenceConventions.Apply(builder));
        Assert.Contains("core entity CoreThing references module 'billing'", ex.Message);
    }

    [Fact]
    public void Modules_must_not_reference_each_other()
    {
        var builder = Build(b =>
        {
            b.Entity<Invoice>();
            b.Entity<Contact>().HasOne(c => c.Invoice).WithMany().HasForeignKey(c => c.InvoiceId);
        });

        var ex = Assert.Throws<InvalidOperationException>(() => ModulePersistenceConventions.Apply(builder));
        Assert.Contains("module 'crm' entity Contact references module 'billing'", ex.Message);
    }

    // The real model must satisfy its own rule -- and today no core entity is module-owned, which
    // is the baseline the first business module builds on.
    [Fact]
    public void Shipped_model_passes_the_boundary_check()
    {
        using var db = TestDbContextFactory.Create(new StubTenant());

        var moduleOwned = db.Model.GetEntityTypes().Where(e => ModulePersistenceConventions.ModuleKeyOf(e.ClrType) is not null).ToList();

        Assert.Empty(moduleOwned);
    }

    // A contributor in a registered assembly gets to add its entities -- the seam a separate module
    // project uses instead of editing ApplicationDbContext.
    public sealed class BillingModelContributor : IModuleModelContributor
    {
        public string ModuleKey => "billing";
        public void Configure(ModelBuilder modelBuilder) => modelBuilder.Entity<Invoice>().ToTable("invoices");
    }

    [Fact]
    public void Registered_module_assembly_contributes_its_entities_under_its_schema()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var modules = new ModulePersistenceOptions();
        modules.ModuleAssemblies.Add(typeof(BillingModelContributor).Assembly);

        using var db = new ApplicationDbContext(options, new StubTenant(), new ProjectNamePlaceholder.Infrastructure.MultiTenancy.CurrentUserService(), modules);

        var invoice = db.Model.FindEntityType(typeof(Invoice));
        Assert.NotNull(invoice);
        Assert.Equal("billing", invoice!.GetSchema());
        // Tenant filter applies to module entities too: ITenantEntity is all the opt-in there is.
        Assert.NotNull(invoice.GetQueryFilter());
    }

    private sealed class StubTenant : ICurrentTenantService
    {
        public long? TenantId => 1;
        public string? TenantSlug => "acme";
        public void Set(long tenantId, string tenantSlug) { }
        public IDisposable BeginScope(long tenantId, string tenantSlug) => throw new NotSupportedException();
        public void Clear() { }
    }
}
