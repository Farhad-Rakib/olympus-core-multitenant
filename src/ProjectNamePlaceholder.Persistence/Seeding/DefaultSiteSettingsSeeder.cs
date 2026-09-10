using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Seeding;

public static class DefaultSiteSettingsSeeder
{
    // tenantName seeds the branding a tenant sees on day one. Without it every tenant started life
    // titled "OlympusCore App" and sending mail signed "OlympusCore" -- the product's name, not
    // theirs -- until an admin found and edited the settings by hand.
    public static async Task SeedAsync(DbContext dbContext, string tenantName)
    {
        var settings = new[]
        {
            new SiteSetting { Key = "Site.Title", Value = tenantName, Description = "Site title" },
            new SiteSetting { Key = "Site.LogoUrl", Value = "/assets/logo.png", Description = "Logo URL" },
            new SiteSetting { Key = "Site.Tagline", Value = "Your productivity, elevated.", Description = "Site tagline" },
            // Empty by default rather than "/favicon.ico": the frontend ships no such file, and
            // applyBranding() skips a blank value instead of pointing the tab icon at a 404.
            new SiteSetting { Key = "Site.FaviconUrl", Value = string.Empty, Description = "Favicon URL" },
            new SiteSetting { Key = "Site.SupportEmail", Value = string.Empty, Description = "Support contact email shown to this tenant's users" },
            // Applied at runtime as a CSS custom property, so a tenant can match their palette
            // without a rebuild (Tailwind's own colors are compile-time).
            new SiteSetting { Key = "UI.BrandColor", Value = "#3b82f6", Description = "Primary brand color (hex)" },
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
            new SiteSetting { Key = "Smtp.FromName", Value = tenantName, Description = "SMTP from name" }
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
