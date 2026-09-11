using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.ModuleSystem;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Seeding
{
    public static class DefaultMenuSeeder
    {
        // Two passes because a module's menu can parent under another module's menu by title (e.g.
        // Platform's "Tenants" parents under Core's "Configuration") -- every menu must exist before
        // any parent link is resolved, so pass order can't depend on module discovery order.
        public static async Task SeedAsync(ApplicationDbContext dbContext, IReadOnlyList<IOlympusModule> modules)
        {
            // Titles are the upsert key, so two modules declaring the same title must collapse to one
            // definition (last wins) -- otherwise the lookup below, which only sees rows already
            // saved, inserts a duplicate. Earlier versions did exactly that; RemoveDuplicateTitles
            // repairs databases seeded by them.
            var definitions = modules.SelectMany(m => m.Menus)
                .GroupBy(d => d.Title, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .ToList();

            await RemoveDuplicateTitlesAsync(dbContext);

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

        // Keeps the oldest row per title, re-points any children of the extras to it, and deletes
        // the extras. Role/permission links are on RequiredPermission (not on the menu row), so
        // nothing else references the removed ids.
        private static async Task RemoveDuplicateTitlesAsync(ApplicationDbContext dbContext)
        {
            var all = await dbContext.Menus.ToListAsync();
            var groups = all.GroupBy(m => m.Title.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1);
            var changed = false;
            foreach (var group in groups)
            {
                var keep = group.OrderBy(m => m.Id).First();
                foreach (var extra in group.Where(m => m.Id != keep.Id))
                {
                    foreach (var child in all.Where(m => m.ParentMenuId == extra.Id))
                    {
                        child.ParentMenuId = keep.Id;
                        child.ParentMenu = keep;
                    }
                    dbContext.Menus.Remove(extra);
                    changed = true;
                }
            }
            if (changed)
            {
                await dbContext.SaveChangesAsync();
            }
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
