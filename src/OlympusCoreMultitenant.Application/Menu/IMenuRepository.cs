using OlympusCoreMultitenant.Domain.Entities;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OlympusCoreMultitenant.Application.Menu
{
    public interface IMenuRepository
    {
        Task<List<OlympusCoreMultitenant.Domain.Entities.Menu>> GetRootMenusWithChildrenAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OlympusCoreMultitenant.Domain.Entities.Menu>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OlympusCoreMultitenant.Domain.Entities.Menu>> GetAllAcrossTenantsAsync(CancellationToken cancellationToken = default);
        Task<OlympusCoreMultitenant.Domain.Entities.Menu?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<OlympusCoreMultitenant.Domain.Entities.Menu> CreateAsync(OlympusCoreMultitenant.Domain.Entities.Menu menu, CancellationToken cancellationToken = default);
        Task UpdateAsync(OlympusCoreMultitenant.Domain.Entities.Menu menu, CancellationToken cancellationToken = default);
        Task DeleteAsync(OlympusCoreMultitenant.Domain.Entities.Menu menu, CancellationToken cancellationToken = default);
    }
}
