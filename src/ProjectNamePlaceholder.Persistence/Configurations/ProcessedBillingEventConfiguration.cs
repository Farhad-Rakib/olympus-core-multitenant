using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Configurations;

public sealed class ProcessedBillingEventConfiguration : IEntityTypeConfiguration<ProcessedBillingEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedBillingEvent> builder)
    {
        builder.ToTable("processed_billing_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExternalEventId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(50).IsRequired();

        // The idempotency guarantee itself -- scoped by provider so switching providers cannot
        // collide with historical event ids.
        builder.HasIndex(x => new { x.Provider, x.ExternalEventId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
