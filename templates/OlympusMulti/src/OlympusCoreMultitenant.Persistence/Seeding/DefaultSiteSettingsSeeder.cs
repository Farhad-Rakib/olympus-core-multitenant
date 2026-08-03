using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Persistence.Seeding;

public static class DefaultSiteSettingsSeeder
{
    public static async Task SeedAsync(DbContext dbContext)
    {
        var settings = new[]
        {
            new SiteSetting { Key = "Site.Title", Value = "OlympusCore App", Description = "Site title" },
            new SiteSetting { Key = "Site.LogoUrl", Value = "/assets/logo.png", Description = "Logo URL" },
            new SiteSetting { Key = "Site.Tagline", Value = "Your productivity, elevated.", Description = "Site tagline" },
            new SiteSetting { Key = "UI.Sidebar.Position", Value = "left", Description = "Sidebar position (left/right)" },
            new SiteSetting { Key = "UI.Sidebar.Collapsed", Value = "false", Description = "Sidebar collapsed by default" },
            new SiteSetting { Key = "UI.Navbar.Freeze", Value = "true", Description = "Navbar freeze enabled" },
            new SiteSetting { Key = "UI.Accordion.Enabled", Value = "true", Description = "Accordion enabled in UI" },
            new SiteSetting { Key = "UI.ColorScheme", Value = "light", Description = "App color scheme (light/dark/auto)" },
            new SiteSetting { Key = "Smtp.Host", Value = "smtp.example.com", Description = "SMTP server host" },
            new SiteSetting { Key = "Smtp.Port", Value = "587", Description = "SMTP server port" },
            new SiteSetting { Key = "Smtp.Username", Value = "user@example.com", Description = "SMTP username" },
            new SiteSetting { Key = "Smtp.Password", Value = "password", Description = "SMTP password" },
            new SiteSetting { Key = "Smtp.FromEmail", Value = "noreply@example.com", Description = "SMTP from email address" },
            new SiteSetting { Key = "Smtp.FromName", Value = "OlympusCore", Description = "SMTP from name" }
        };

        foreach (var setting in settings)
        {
            if (!await dbContext.Set<SiteSetting>().AnyAsync(s => s.Key == setting.Key))
            {
                dbContext.Add(setting);
            }
        }
        await dbContext.SaveChangesAsync();
    }
}
