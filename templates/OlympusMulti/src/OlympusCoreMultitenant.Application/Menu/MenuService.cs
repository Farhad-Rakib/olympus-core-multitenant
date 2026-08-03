using System.Security.Claims;
using OlympusCoreMultitenant.Application.Common;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;

namespace OlympusCoreMultitenant.Application.Menu
{
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _menuRepository;
        private readonly OlympusCoreMultitenant.Application.Common.Interfaces.IAppCache _cache;
        private readonly ICurrentTenantService _currentTenantService;

        private readonly TimeSpan MenuCacheTtl;

        public MenuService(
            IMenuRepository menuRepository,
            OlympusCoreMultitenant.Application.Common.Interfaces.IAppCache cache,
            ICurrentTenantService currentTenantService,
            Microsoft.Extensions.Options.IOptions<OlympusCoreMultitenant.Application.Common.Configuration.CachingOptions> cachingOptions)
        {
            _menuRepository = menuRepository;
            _cache = cache;
            _currentTenantService = currentTenantService;
            MenuCacheTtl = TimeSpan.FromMinutes(cachingOptions?.Value?.MenusTtlMinutes ?? 30);
        }

        private string CacheKey(string key) =>
            TenantCacheKeys.For(_currentTenantService.TenantId ?? throw new TenantContextMissingException(), key);

        public async Task<List<MenuItemDto>> GetMenuForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            var userPermissions = user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet();

            // Try cached tree first
            var cached = await _cache.GetAsync<List<MenuDto>>(CacheKey("menus:tree"), cancellationToken);
            List<Domain.Entities.Menu> allMenus;
            if (cached is not null && cached.Count > 0)
            {
                // Convert cached DTOs back to domain-like structure for filtering
                allMenus = cached.Select(d => new Domain.Entities.Menu
                {
                    Id = d.Id,
                    Title = d.Title,
                    Url = d.Url,
                    Icon = d.Icon,
                    RequiredPermission = d.RequiredPermission,
                    ParentMenuId = d.ParentMenuId,
                    Children = d.Children != null ? d.Children.Select(c => MapDtoToEntity(c)).ToList() : new List<Domain.Entities.Menu>()
                }).ToList();
            }
            else
            {
                allMenus = await _menuRepository.GetRootMenusWithChildrenAsync(cancellationToken);
                // Cache DTO representation
                var dtoTree = allMenus.Select(MapToDto).ToList();
                await _cache.SetAsync(CacheKey("menus:tree"), dtoTree, MenuCacheTtl, cancellationToken);
            }

            return FilterMenuItems(allMenus, userPermissions);
        }

        private List<MenuItemDto> FilterMenuItems(List<Domain.Entities.Menu> items, HashSet<string> userPermissions)
        {
            return items
                .Where(item => string.IsNullOrEmpty(item.RequiredPermission) || userPermissions.Contains(item.RequiredPermission))
                .Select(item => new MenuItemDto
                {
                    Title = item.Title,
                    Url = item.Url,
                    Icon = item.Icon,
                    RequiredPermission = item.RequiredPermission,
                    Children = item.Children != null ? FilterMenuItems(item.Children, userPermissions) : null
                })
                .ToList();
        }

        public async Task<List<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<List<MenuDto>>(CacheKey("menus:all"), cancellationToken);
            if (cached is not null && cached.Count > 0)
                return cached;

            var all = await _menuRepository.GetAllAsync(cancellationToken);
            var result = all.Select(MapToDto).ToList();
            await _cache.SetAsync(CacheKey("menus:all"), result, MenuCacheTtl, cancellationToken);
            return result;
        }

        public async Task<MenuDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
            return menu == null ? null : MapToDto(menu);
        }

        public async Task<MenuDto> CreateAsync(CreateMenuRequestDto request, CancellationToken cancellationToken = default)
        {
            var entity = new Domain.Entities.Menu
            {
                Title = request.Title,
                Url = request.Url,
                Icon = request.Icon,
                RequiredPermission = request.RequiredPermission,
                ParentMenuId = request.ParentMenuId
            };

            var created = await _menuRepository.CreateAsync(entity, cancellationToken);
            // Invalidate caches
            await _cache.RemoveAsync(CacheKey("menus:tree"), cancellationToken);
            await _cache.RemoveAsync(CacheKey("menus:all"), cancellationToken);
            return MapToDto(created);
        }

        public async Task UpdateAsync(long id, UpdateMenuRequestDto request, CancellationToken cancellationToken = default)
        {
            var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
            if (menu == null) throw new KeyNotFoundException("Menu not found");

            menu.Title = request.Title;
            menu.Url = request.Url;
            menu.Icon = request.Icon;
            menu.RequiredPermission = request.RequiredPermission;
            menu.ParentMenuId = request.ParentMenuId;

            await _menuRepository.UpdateAsync(menu, cancellationToken);
            await _cache.RemoveAsync(CacheKey("menus:tree"), cancellationToken);
            await _cache.RemoveAsync(CacheKey("menus:all"), cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
            if (menu == null) throw new KeyNotFoundException("Menu not found");

            await _menuRepository.DeleteAsync(menu, cancellationToken);
            await _cache.RemoveAsync(CacheKey("menus:tree"), cancellationToken);
            await _cache.RemoveAsync(CacheKey("menus:all"), cancellationToken);
        }

        private MenuDto MapToDto(Domain.Entities.Menu m)
        {
            return new MenuDto
            {
                Id = m.Id,
                Title = m.Title,
                Url = m.Url,
                Icon = m.Icon,
                RequiredPermission = m.RequiredPermission,
                ParentMenuId = m.ParentMenuId,
                Children = m.Children?.Select(MapToDto).ToList()
            };
        }

        private static Domain.Entities.Menu MapDtoToEntity(MenuDto d)
        {
            return new Domain.Entities.Menu
            {
                Id = d.Id,
                Title = d.Title,
                Url = d.Url,
                Icon = d.Icon,
                RequiredPermission = d.RequiredPermission,
                ParentMenuId = d.ParentMenuId,
                Children = d.Children?.Select(MapDtoToEntity).ToList() ?? new List<Domain.Entities.Menu>()
            };
        }
    }
}
