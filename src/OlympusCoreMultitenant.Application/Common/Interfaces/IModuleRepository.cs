using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface IModuleRepository : IRepository<Module>
{
    Task<Module?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}
