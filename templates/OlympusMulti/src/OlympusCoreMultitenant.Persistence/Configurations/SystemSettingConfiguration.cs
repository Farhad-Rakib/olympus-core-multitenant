using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(150).IsRequired();
        builder.Property(setting => setting.Value).IsRequired();
        builder.Property(setting => setting.Description).HasMaxLength(500);

        builder.HasIndex(setting => setting.Key).IsUnique();
    }
}
