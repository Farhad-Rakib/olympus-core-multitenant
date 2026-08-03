using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Menu;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories
{
    public class MenuRepository : BaseRepository<Menu>, IMenuRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public MenuRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Menu>> GetRootMenusWithChildrenAsync(CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .AsNoTracking()
                .Where(m => m.TenantId == DbContext.CurrentTenantId)
                .Include(m => m.Children)
                .Where(m => m.ParentMenuId == null)
                .ToListAsync(cancellationToken);
        }

        public override async Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .AsNoTracking()
                .Where(m => m.TenantId == DbContext.CurrentTenantId)
                .Include(m => m.Children)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Menu>> GetAllAcrossTenantsAsync(CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(m => m.Children)
                .ToListAsync(cancellationToken);
        }

        // Explicit TenantId filter: see UserRepository.GetByEmailAsync for why the automatic global
        // query filter is not sufficient for Include()+FirstOrDefaultAsync on this shape.
        public override async Task<Menu?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .Where(m => m.TenantId == DbContext.CurrentTenantId)
                .Include(m => m.Children)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<Menu> CreateAsync(Menu menu, CancellationToken cancellationToken = default)
        {
            await AddAsync(menu, cancellationToken);
            return menu;
        }

        public Task UpdateAsync(Menu menu, CancellationToken cancellationToken = default)
        {
            Update(menu);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Menu menu, CancellationToken cancellationToken = default)
        {
            Delete(menu);
            return Task.CompletedTask;
        }
    }
}
