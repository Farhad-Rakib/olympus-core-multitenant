using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class ModuleRepository : BaseRepository<Module>, IModuleRepository
{
    public ModuleRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Module?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DbContext.Modules
            .FirstOrDefaultAsync(module => module.Key == key, cancellationToken);
    }
}
