using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.MaxUsers).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Explicit precision: without it EF maps decimal to the provider default, which silently
        // truncates on SQL Server. 18,2 covers any realistic subscription price.
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.BillingInterval).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExternalPriceId).HasMaxLength(200);

        // Looked up when a webhook names a provider price. No explicit filter: Postgres treats NULLs
        // as distinct in a unique index, and the SQL Server provider adds "IS NOT NULL" itself, so
        // the many plans without a provider price never collide on either. (HasFilter(null) would
        // REMOVE SQL Server's filter and make a second unpriced plan fail to insert.)
        builder.HasIndex(x => x.ExternalPriceId).IsUnique();

        builder.HasIndex(x => x.Key).IsUnique();
    }
}
