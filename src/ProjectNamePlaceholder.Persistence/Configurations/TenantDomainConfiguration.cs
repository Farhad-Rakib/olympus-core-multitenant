using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Configurations;

public sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("tenant_domains");

        builder.HasKey(domain => domain.Id);
        builder.Property(domain => domain.TenantId).IsRequired();
        builder.Property(domain => domain.Domain).HasMaxLength(255).IsRequired();

        builder.HasIndex(domain => domain.Domain).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(domain => domain.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
