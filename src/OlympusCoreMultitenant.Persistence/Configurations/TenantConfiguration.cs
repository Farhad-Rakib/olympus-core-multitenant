using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
