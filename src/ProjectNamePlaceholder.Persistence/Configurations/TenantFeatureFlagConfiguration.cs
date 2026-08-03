using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Configurations;

public sealed class TenantFeatureFlagConfiguration : IEntityTypeConfiguration<TenantFeatureFlag>
{
    public void Configure(EntityTypeBuilder<TenantFeatureFlag> builder)
    {
        builder.ToTable("tenant_feature_flags");

        builder.HasKey(flag => flag.Id);
        builder.Property(flag => flag.TenantId).IsRequired();
        builder.Property(flag => flag.Key).HasMaxLength(150).IsRequired();
        builder.Property(flag => flag.IsEnabled).IsRequired();
        builder.Property(flag => flag.RolloutPercentage).IsRequired();

        builder.HasIndex(flag => new { flag.TenantId, flag.Key }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(flag => flag.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
