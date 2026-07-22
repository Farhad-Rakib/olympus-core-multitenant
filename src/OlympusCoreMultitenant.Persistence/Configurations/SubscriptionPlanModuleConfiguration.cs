using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Configurations;

public sealed class SubscriptionPlanModuleConfiguration : IEntityTypeConfiguration<SubscriptionPlanModule>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanModule> builder)
    {
        builder.ToTable("subscription_plan_modules");
        builder.HasKey(x => new { x.SubscriptionPlanId, x.ModuleId });

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Module)
            .WithMany()
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
