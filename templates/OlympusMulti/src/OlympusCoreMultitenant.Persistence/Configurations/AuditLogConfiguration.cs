using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.CreatedAt);
    }
}
