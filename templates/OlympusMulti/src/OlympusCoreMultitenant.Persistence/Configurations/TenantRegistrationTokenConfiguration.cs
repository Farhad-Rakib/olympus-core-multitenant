using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Configurations;

public sealed class TenantRegistrationTokenConfiguration : IEntityTypeConfiguration<TenantRegistrationToken>
{
    public void Configure(EntityTypeBuilder<TenantRegistrationToken> builder)
    {
        builder.ToTable("tenant_registration_tokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PendingFullName).HasMaxLength(200);
        builder.Property(x => x.PendingEmail).HasMaxLength(320);
        builder.Property(x => x.PendingPasswordHash).HasMaxLength(500);

        builder.HasIndex(x => x.Token).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
