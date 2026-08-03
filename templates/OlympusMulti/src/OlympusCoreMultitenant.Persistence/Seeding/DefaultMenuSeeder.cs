using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.ModuleSystem;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Seeding
{
    public static class DefaultMenuSeeder
    {
        // Two passes because a module's menu can parent under another module's menu by title (e.g.
        // Platform's "Tenants" parents under Core's "Configuration") -- every menu must exist before
        // any parent link is resolved, so pass order can't depend on module discovery order.
        public static async Task SeedAsync(ApplicationDbContext dbContext, IReadOnlyList<IOlympusModule> modules)
        {
            var definitions = modules.SelectMany(m => m.Menus).ToList();

            var menusByTitle = new Dictionary<string, Menu>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                menusByTitle[definition.Title] = await UpsertMenuAsync(dbContext, definition.Title, definition.Url, definition.Icon, definition.RequiredPermission);
            }

            foreach (var definition in definitions)
            {
                if (definition.ParentTitle is null)
                {
                    continue;
                }

                if (!menusByTitle.TryGetValue(definition.ParentTitle, out var parent))
                {
                    continue;
                }

                var menu = menusByTitle[definition.Title];
                menu.ParentMenu = parent;
                menu.ParentMenuId = parent.Id;
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task<Menu> UpsertMenuAsync(
            ApplicationDbContext dbContext,
            string title,
            string? url,
            string? icon,
            string? requiredPermission)
        {
            var menu = await dbContext.Menus.FirstOrDefaultAsync(existing => existing.Title == title);
            if (menu is null)
            {
                menu = new Menu { Title = title };
                dbContext.Menus.Add(menu);
            }

            menu.Url = url;
            menu.Icon = icon;
            menu.RequiredPermission = requiredPermission;

            return menu;
        }
    }
}
