using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Configuration;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Seeding;

public static class DefaultSystemSettingsSeeder
{
    public static async Task SeedAsync(DbContext dbContext)
    {
        if (!await dbContext.Set<SystemSetting>().AnyAsync(s => s.Key == SystemSettingKeys.AuthEmailUniquenessScope))
        {
            dbContext.Add(new SystemSetting
            {
                Key = SystemSettingKeys.AuthEmailUniquenessScope,
                Value = "PerTenant",
                Description = "Controls whether login requires a tenant name (PerTenant) or only email (Global)."
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
