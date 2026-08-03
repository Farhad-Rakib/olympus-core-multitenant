using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IModuleRepository : IRepository<Module>
{
    Task<Module?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}
